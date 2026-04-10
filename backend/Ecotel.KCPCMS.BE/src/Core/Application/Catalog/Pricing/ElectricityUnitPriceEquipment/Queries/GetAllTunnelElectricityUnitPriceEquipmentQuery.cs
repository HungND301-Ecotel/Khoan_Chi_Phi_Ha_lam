using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Common.Enums;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record GetAllTunnelElectricityUnitPriceEquipmentQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination,
    ElectricityUnitPriceType Type = ElectricityUnitPriceType.TunnelExcavation) : IRequest<PaginationResponse<ElectricityUnitPriceEquipmentDto>>;

public class GetAllTunnelElectricityUnitPriceEquipmentQueryHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllTunnelElectricityUnitPriceEquipmentQuery, PaginationResponse<ElectricityUnitPriceEquipmentDto>>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    public async Task<PaginationResponse<ElectricityUnitPriceEquipmentDto>> Handle(GetAllTunnelElectricityUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        // Query only Tunnel type
        var query = _repository.GetAll()
            .Where(e => e.ElectricityType == request.Type)
            .Include(e => e.Equipment).ThenInclude(e => e!.Code)
            .Include(e => e.Equipment).ThenInclude(e => e!.UnitOfMeasure)
            .Include(e => e.Equipment).ThenInclude(e => e!.Costs)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(e =>
                e.Equipment!.Name.Contains(request.Search) ||
                e.Equipment.Code!.Value.Contains(request.Search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (!filter.IgnorePagination)
        {
            query = query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        var data = await query.ToListAsync(cancellationToken);

        var listData = data.Select(e => new ElectricityUnitPriceEquipmentDto
        {
            Id = e.Id,
            EquipmentId = e.EquipmentId,
            EquipmentCode = e.Equipment!.Code?.Value ?? "",
            EquipmentName = e.Equipment.Name,
            UnitOfMeasureName = e.Equipment.UnitOfMeasure?.Name ?? "",
            ElectricityConsumePerMetres = e.GetElectricityConsumePerMetres(),
            ElectricityCostPerMetres = e.GetElectricityCostPerMetres(),
            EquipmentElectricityCost = e.GetCurrentElectricityCost(),
            StartMonth = e.StartMonth,
            EndMonth = e.EndMonth,
            Type = e.ElectricityType,
            MonthlyElectricityCost = e is TunnelElectricityUnitPriceEquipment tunnel ? tunnel.MonthlyElectricityCost : null,
            AverageMonthlyTunnelProduction = e is TunnelElectricityUnitPriceEquipment tunnel2 ? tunnel2.AverageMonthlyTunnelProduction : null
        }).ToList();

        return new PaginationResponse<ElectricityUnitPriceEquipmentDto>(
            listData,
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }
}
