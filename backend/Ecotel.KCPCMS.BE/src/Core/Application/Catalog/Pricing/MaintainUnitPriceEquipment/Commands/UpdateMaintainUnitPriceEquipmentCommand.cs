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

    public async Task<bool> Handle(UpdateMaintainUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existMaintainUnitPrice = await _maintainUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.EquipmentId == request.UpdateModel.EquipmentId,
            include: t => t.Include(t => t.MaintainUnitPriceEquipments),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

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
                partPrice.AverageMonthlyTunnelProduction
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

            _maintainUnitPriceRepository.Update(existMaintainUnitPrice);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            cacheService.InvalidateGroup(CacheSignalKey);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
        return true;
    }
}
