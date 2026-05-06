using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;

public record UpdateLowValuePerishableSupplyUnitPriceCommand(UpdateLowValuePerishableSupplyUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateLowValuePerishableSupplyUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<UpdateLowValuePerishableSupplyUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(UpdateLowValuePerishableSupplyUnitPriceCommand request, CancellationToken cancellationToken)
    {
        bool departmentExists = await _departmentRepository.ExistsAsync(d => d.Id == request.UpdateModel.DepartmentId);
        if (!departmentExists)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        ProcessGroup processGroup = await _processGroupRepository.GetFirstOrDefaultAsync(
            predicate: pg => pg.Id == request.UpdateModel.ProcessGroupId,
            include: pg => pg.Include(x => x.FixedKey),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);

        ValidateProcessGroupType(request.UpdateModel.Type, processGroup);

        bool overlapExists = await _repository.ExistsAsync(e =>
            e.DepartmentId == request.UpdateModel.DepartmentId &&
            e.ProcessGroupId == request.UpdateModel.ProcessGroupId &&
            e.Type == request.UpdateModel.Type &&
            e.Id != request.UpdateModel.Id &&
            e.StartMonth <= request.UpdateModel.EndMonth &&
            e.EndMonth >= request.UpdateModel.StartMonth);

        if (overlapExists)
        {
            throw new ConflictException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceAlreadyExists);
        }

        Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice entity = await _repository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceNotFound);

        entity.Update(
            request.UpdateModel.DepartmentId,
            request.UpdateModel.ProcessGroupId,
            request.UpdateModel.StartMonth,
            request.UpdateModel.EndMonth,
            request.UpdateModel.Type,
            request.UpdateModel.TotalPrice);

        _repository.Update(entity);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);
        cacheService.InvalidateGroup(ModuleCacheSignalKey);
        return true;
    }

    private static void ValidateProcessGroupType(LowValuePerishableSupplyType type, ProcessGroup processGroup)
    {
        ProcessGroupType expectedType = type switch
        {
            LowValuePerishableSupplyType.TunnelExcavation => ProcessGroupType.DL,
            LowValuePerishableSupplyType.Longwall => ProcessGroupType.LC,
            _ => ProcessGroupType.None,
        };

        var actualType = processGroup.FixedKey?.Type.ToProcessGroupType() ?? ProcessGroupType.None;

        if (actualType != expectedType)
        {
            throw new ConflictException(CustomResponseMessage.ProcessGroupNotFound);
        }
    }
}