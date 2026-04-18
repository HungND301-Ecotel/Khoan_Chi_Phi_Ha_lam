using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Domain.Common.Enums;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Queries;

public record GetAllLongwallElectricityUnitPriceEquipmentQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<ElectricityUnitPriceEquipmentDto>>;

public class GetAllLongwallElectricityUnitPriceEquipmentQueryHandler(
    IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllLongwallElectricityUnitPriceEquipmentQuery, PaginationResponse<ElectricityUnitPriceEquipmentDto>>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();

    public async Task<PaginationResponse<ElectricityUnitPriceEquipmentDto>> Handle(GetAllLongwallElectricityUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var filter = new PaginationFilter
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination
        };

        // Query only Longwall type
        var query = _repository.GetAll()
            .Where(e => e.ElectricityType == ElectricityUnitPriceType.Longwall)
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

        var data = await query.ToListAsync(cancellationToken);
        IEnumerable<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> sortedData = data
            .OrderByCodeNatural(e => e.Equipment!.Code!.Value)
            .ThenBy(e => e.Equipment!.Name);

        if (!filter.IgnorePagination)
        {
            sortedData = sortedData
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        var listData = sortedData.Select(e => new ElectricityUnitPriceEquipmentDto
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
            Quantity = e is LongwallElectricityUnitPriceEquipment longwall ? longwall.Quantity : null,
            Pdm = e is LongwallElectricityUnitPriceEquipment longwall2 ? longwall2.Pdm : null,
            Kyc = e is LongwallElectricityUnitPriceEquipment longwall3 ? longwall3.Kyc : null,
            Kdt = e is LongwallElectricityUnitPriceEquipment longwall4 ? longwall4.Kdt : null,
            WorkingHour = e is LongwallElectricityUnitPriceEquipment longwall5 ? longwall5.WorkingHour : null,
            WorkingDate = e is LongwallElectricityUnitPriceEquipment longwall6 ? longwall6.WorkingDate : null,
            LongwallAverageMonthlyTunnelProduction = e is LongwallElectricityUnitPriceEquipment longwall7 ? longwall7.AverageMonthlyTunnelProduction : null,
            SPdm = e is LongwallElectricityUnitPriceEquipment lwall ? lwall.SPdm : null,
            Ptt = e is LongwallElectricityUnitPriceEquipment lwallPtt ? lwallPtt.Ptt : null
        }).ToList();

        return new PaginationResponse<ElectricityUnitPriceEquipmentDto>(
            listData,
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }
}
