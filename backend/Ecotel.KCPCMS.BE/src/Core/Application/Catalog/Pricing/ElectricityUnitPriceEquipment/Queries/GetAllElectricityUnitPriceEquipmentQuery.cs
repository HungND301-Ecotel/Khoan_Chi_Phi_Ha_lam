using Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Specifications;
using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Persistence;
using Application.Common.Services;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record GetAllElectricityUnitPriceEquipmentQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ElectricityUnitPriceEquipmentDto>>;

public class GetAllUnitPriceQueryHandler(IPaginationService paginationService, IReadRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> electricityUnitPriceEquipmentRepository, ICacheService cacheService) : IRequestHandler<GetAllElectricityUnitPriceEquipmentQuery, PaginationResponse<ElectricityUnitPriceEquipmentDto>>
{
    private const string CacheSignalKey = "ElectricityUnitPriceEquipment";

    public async Task<PaginationResponse<ElectricityUnitPriceEquipmentDto>> Handle(GetAllElectricityUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"GetAllElectricityUnitPriceEquipment:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}";
        var cachedResult = await cacheService.GetAsync<PaginationResponse<ElectricityUnitPriceEquipmentDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        var spec = new ElectricityUnitPricesByPaginationSpec(filter, request.Search);

        var paginationResponse = await paginationService.PaginatedListAsync(
            repository: electricityUnitPriceEquipmentRepository,
            spec: spec,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize,
            ignorePagination: filter.IgnorePagination,
            cancellationToken: cancellationToken);

        var listData = paginationResponse.Data.Select(e => new ElectricityUnitPriceEquipmentDto
        {
            Id = e.Id,
            EquipmentId = e.EquipmentId,
            EquipmentCode = e.Equipment!.Code?.Value ?? "",
            EquipmentName = e.Equipment.Name,
            ProcessGroupTypes = e.Equipment.EquipmentProcessGroups
                .Where(epg => epg.ProcessGroup != null)
                .Select(epg => epg.ProcessGroup.Type)
                .Distinct()
                .ToList(),
            UnitOfMeasureName = e.Equipment.UnitOfMeasure != null ? e.Equipment.UnitOfMeasure.Name : string.Empty,
            ElectricityConsumePerMetres = e.GetElectricityConsumePerMetres(),
            ElectricityCostPerMetres = e.GetElectricityCostPerMetres(),
            EquipmentElectricityCost = e.GetCurrentElectricityCost(),
            StartMonth = e.StartMonth,
            EndMonth = e.EndMonth,
            Type = e.ElectricityType,
            // Tunnel properties
            MonthlyElectricityCost = e is TunnelElectricityUnitPriceEquipment tunnel ? tunnel.MonthlyElectricityCost : null,
            AverageMonthlyTunnelProduction = e is TunnelElectricityUnitPriceEquipment tunnel2 ? tunnel2.AverageMonthlyTunnelProduction : null,
            // Longwall properties
            Quantity = e is LongwallElectricityUnitPriceEquipment longwall ? longwall.Quantity : null,
            Pdm = e is LongwallElectricityUnitPriceEquipment longwall2 ? longwall2.Pdm : null,
            Kyc = e is LongwallElectricityUnitPriceEquipment longwall3 ? longwall3.Kyc : null,
            Kdt = e is LongwallElectricityUnitPriceEquipment longwall4 ? longwall4.Kdt : null,
            WorkingHour = e is LongwallElectricityUnitPriceEquipment longwall5 ? longwall5.WorkingHour : null,
            WorkingDate = e is LongwallElectricityUnitPriceEquipment longwall6 ? longwall6.WorkingDate : null,
            LongwallAverageMonthlyTunnelProduction = e is LongwallElectricityUnitPriceEquipment longwall7 ? longwall7.AverageMonthlyTunnelProduction : null,
            // Longwall calculated properties
            SPdm = e is LongwallElectricityUnitPriceEquipment lwallSPdm ? lwallSPdm.SPdm : null,
            Ptt = e is LongwallElectricityUnitPriceEquipment lwallPtt ? lwallPtt.Ptt : null
        })
        .OrderByCodeNatural(d => d.EquipmentCode)
        .ThenBy(d => d.EquipmentName)
        .ToList();

        var result = new PaginationResponse<ElectricityUnitPriceEquipmentDto>(listData, paginationResponse.TotalCount, paginationResponse.CurrentPage, paginationResponse.PageSize);
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);

        return result;
    }
}
