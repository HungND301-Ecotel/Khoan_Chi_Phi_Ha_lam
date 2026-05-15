using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaintainUnitPriceEquipment;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record GetMaintainUnitPriceEquipmentByIdQuery(DefaultIdType Id) : IRequest<MaintainUnitPriceDto>;

public class GetMaintainUnitPriceEquipmentByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMaintainUnitPriceEquipmentByIdQuery, MaintainUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPrice> _maintainUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPrice>();
    public async Task<MaintainUnitPriceDto> Handle(GetMaintainUnitPriceEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var maintainUnitPrice = await _maintainUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.Id,
            include: m => m
                .Include(m => m.MaintainUnitPriceEquipments).ThenInclude(e => e.Part).ThenInclude(p => p.Costs)
                .Include(m => m.MaintainUnitPriceEquipments).ThenInclude(e => e.Part).ThenInclude(p => p.Code)
                .Include(m => m.MaintainUnitPriceEquipments).ThenInclude(e => e.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(m => m.Equipment).ThenInclude(e => e.Code!),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var costs = maintainUnitPrice?.MaintainUnitPriceEquipments.Select(x =>
            new MaintainUnitPriceEquipmentDto
            {
                Id = x.Id,
                EquipmentCode = maintainUnitPrice.Equipment?.Code?.Value ?? "",
                PartName = x.Part?.Name ?? "",
                PartCode = x.Part?.Code?.Value ?? "",
                PartId = x.PartId,
                PartType = x.Part?.MaterialType == Domain.Common.Enums.MaterialType.MaterialOutContract
                    ? Domain.Common.Enums.PartType.OtherPart
                    : Domain.Common.Enums.PartType.Part,
                EquipmentId = maintainUnitPrice.EquipmentId,
                Quantity = x.Quantity,
                PartCost = x.Part?.GetMaterialCost(maintainUnitPrice.StartMonth) ?? 0,
                UnitOfMeasureId = x.Part?.UnitOfMeasureId,
                UnitOfMeasureName = x.Part?.UnitOfMeasure?.Name ?? "",
                ReplacementTimeStandard = x.ReplacementTimeStandard,
                AverageMonthlyTunnelProduction = x.AverageMonthlyTunnelProduction,
                MaterialCostPerMetres = x.GetMaterialCostPerMetres(maintainUnitPrice.StartMonth, maintainUnitPrice.Type),
                MaterialRatePerMetres = x.GetMaterialRate()
            }).ToList();

        double totalPrice = maintainUnitPrice.GetMaintainTotalPrice();

        return new MaintainUnitPriceDto
        {
            Id = maintainUnitPrice.Id,
            EquipmentId = maintainUnitPrice?.EquipmentId ?? DefaultIdType.Empty,
            EquipmentCode = maintainUnitPrice?.Equipment?.Code?.Value ?? "",
            TotalPrice = totalPrice,
            StartMonth = maintainUnitPrice.StartMonth,
            EndMonth = maintainUnitPrice.EndMonth,
            OtherMaterialValue = maintainUnitPrice.OtherMaterialValue,
            Type = maintainUnitPrice.Type,
            MaintainUnitPriceEquipment = costs.OrderBy(c => c.PartCode).ThenBy(c => c.PartName).ToList() ?? []
        };
    }
}

