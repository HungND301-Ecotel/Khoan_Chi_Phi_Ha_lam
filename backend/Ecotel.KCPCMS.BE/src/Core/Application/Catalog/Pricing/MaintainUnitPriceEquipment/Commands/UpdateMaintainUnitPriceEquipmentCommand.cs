using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record UpdateMaintainUnitPriceEquipmentCommand(UpdateMaintainUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateMaintainUnitPriceEquipmentCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<UpdateMaintainUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPrice> _maintainUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPrice>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPriceEquipment>();

    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "MaintainUnitPriceEquipment";

    public async Task<bool> Handle(UpdateMaintainUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        var normalizedStartMonth = new DateOnly(request.UpdateModel.StartMonth.Year, request.UpdateModel.StartMonth.Month, 1);
        var normalizedEndMonth = new DateOnly(request.UpdateModel.EndMonth.Year, request.UpdateModel.EndMonth.Month, 1);

        var maintainUnitPriceId = request.UpdateModel.Id.GetValueOrDefault();
        var existMaintainUnitPrice = await _maintainUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t =>
                request.UpdateModel.Id.HasValue && maintainUnitPriceId != Guid.Empty
                    ? t.Id == maintainUnitPriceId
                    : t.EquipmentId == request.UpdateModel.EquipmentId &&
                      t.Type == request.UpdateModel.Type &&
                      t.StartMonth == normalizedStartMonth &&
                      t.EndMonth == normalizedEndMonth,
            include: t => t.Include(t => t.MaintainUnitPriceEquipments),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.MaintainUnitPriceNotFound);

        if (await _maintainUnitPriceRepository.AnyAsync(t =>
            t.Id != existMaintainUnitPrice.Id &&
            t.EquipmentId == request.UpdateModel.EquipmentId &&
            t.Type == request.UpdateModel.Type &&
            t.StartMonth <= normalizedEndMonth &&
            t.EndMonth >= normalizedStartMonth))
        {
            throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
        }

        var equipmentDetail = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.EquipmentId,
            include: e => e.Include(e => e.EquipmentParts).ThenInclude(ep => ep.Part).ThenInclude(p => p.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);

        if (equipmentDetail?.EquipmentParts == null || !equipmentDetail.EquipmentParts.Any())
        {
            throw new ConflictException(CustomResponseMessage.EquipmentPartsInvalid);
        }

        var mantainUnitPriceEquipment = new List<Domain.Entities.Pricing.MaintainUnitPriceEquipment>();

        foreach (var partPrice in request.UpdateModel.PartUnitPrices)
        {
            mantainUnitPriceEquipment.Add(Domain.Entities.Pricing.MaintainUnitPriceEquipment.Create(
                null,
                partPrice.PartId,
                partPrice.Quantity,
                partPrice.AverageMonthlyTunnelProduction,
                partPrice.ReplacementTimeStandard
            ));
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _maintainUnitPriceEquipmentRepository.Delete(existMaintainUnitPrice.MaintainUnitPriceEquipments);
            existMaintainUnitPrice.ClearMaintainUnitPriceEquipment();

            existMaintainUnitPrice.Update(
                request.UpdateModel.EquipmentId,
                request.UpdateModel.StartMonth,
                request.UpdateModel.EndMonth,
                mantainUnitPriceEquipment,
                request.UpdateModel.OtherMaterialValue,
                request.UpdateModel.Type);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            cacheService.InvalidateGroup(CacheSignalKey);
            cacheService.InvalidateGroup(ModuleCacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
    }
}

