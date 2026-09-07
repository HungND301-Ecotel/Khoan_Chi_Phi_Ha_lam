using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.Services;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LongwallParameters;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record class GetGroupedLongwallMaterialUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination) : IRequest<PaginationResponse<GroupedLongwallMaterialUnitPriceDto>>;

public class GetGroupedLongwallMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<GetGroupedLongwallMaterialUnitPriceQuery, PaginationResponse<GroupedLongwallMaterialUnitPriceDto>>
{
    private const string CacheSignalKey = "LongwallMaterialUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();

    public async Task<PaginationResponse<GroupedLongwallMaterialUnitPriceDto>> Handle(
        GetGroupedLongwallMaterialUnitPriceQuery request,
        CancellationToken cancellationToken)
    {
        var pageNumber = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize == 0 ? int.MaxValue : request.PageSize > 0 ? request.PageSize : 10;
        var searchTerm = request.Search?.Trim();
        var normalizedSearchTerm = searchTerm?.ToLower();
        var cacheKey = $"{CacheSignalKey}:Grouped:{pageNumber}:{pageSize}:{normalizedSearchTerm ?? "empty"}:{request.IgnorePagination}";

        var cachedResult = await cacheService.GetAsync<PaginationResponse<GroupedLongwallMaterialUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var baseQuery = _materialUnitPriceRepository.GetAll()
            .Where(m => string.IsNullOrWhiteSpace(normalizedSearchTerm) ||
                        m.Code != null && m.Code.Value.ToLower().Contains(normalizedSearchTerm));

        var allBaseData = await baseQuery
            .Select(m => new LongwallMaterialUnitPriceBaseData
            {
                Id = m.Id,
                Code = m.Code != null ? m.Code.Value : string.Empty,
                ProcessId = m.ProcessId,
                ProcessName = m.ProductionProcess != null ? m.ProductionProcess.Name : string.Empty,
                LongwallParametersId = m.LongwallParametersId,
                CuttingThicknessId = m.CuttingThicknessId,
                SeamFaceId = m.SeamFaceId,
                TechnologyId = m.TechnologyId,
                PowerId = m.PowerId,
                HardnessId = m.HardnessId,
                PowerName = m.PowerId == null || m.Power == null ? string.Empty : m.Power.Value,
                HardnessName = m.HardnessId == null || m.Hardness == null ? string.Empty : m.Hardness.Value,
                IsLongwallMaterialUnitPriceCGH = m.IsLongwallMaterialUnitPriceCGH,
                TechnologyName = m.Technology != null ? m.Technology.Value : string.Empty,
                LongwallParameters = m.LongwallParameters == null
                    ? new LongwallParametersDto()
                    : new LongwallParametersDto
                    {
                        Id = m.LongwallParameters.Id,
                        Llc = m.LongwallParameters.Llc,
                        Lkc = m.LongwallParameters.Lkc,
                        Mk = m.LongwallParameters.Mk
                    },
                CuttingThickness = m.CuttingThickness == null
                    ? new CuttingThicknessDto()
                    : new CuttingThicknessDto
                    {
                        Id = m.CuttingThickness.Id,
                        Value = m.CuttingThickness.Value
                    },
                SeamFaceName = m.SeamFace != null ? m.SeamFace.Value : string.Empty,
                StartMonth = m.StartMonth,
                EndMonth = m.EndMonth,
                OtherMaterialValue = m.OtherMaterialvalue
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var allIds = allBaseData.Select(m => m.Id).ToList();
        var costs = await _materialUnitPriceRepository.GetAll()
            .Where(m => allIds.Contains(m.Id))
            .SelectMany(m => m.MaterialUnitPriceAssignmentCodes.Select(cost => new
            {
                MaterialUnitPriceId = m.Id,
                cost.TotalPrice
            }))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var costTotalsByEntityId = costs
            .GroupBy(c => c.MaterialUnitPriceId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalPrice));

        var grouped = allBaseData
            .GroupBy(d => new
            {
                d.Code,
                d.ProcessId,
                d.LongwallParametersId,
                d.CuttingThicknessId,
                d.SeamFaceId,
                d.TechnologyId,
                d.PowerId,
                d.HardnessId,
                d.IsLongwallMaterialUnitPriceCGH
            })
            .Select(g =>
            {
                var first = g.OrderByDescending(p => p.StartMonth).First();
                return new GroupedLongwallMaterialUnitPriceDto
                {
                    Id = first.Id,
                    Code = g.Key.Code,
                    ProcessId = g.Key.ProcessId,
                    ProcessName = first.ProcessName,
                    LongwallParametersId = g.Key.LongwallParametersId,
                    CuttingThicknessId = g.Key.CuttingThicknessId,
                    SeamFaceId = g.Key.SeamFaceId,
                    TechnologyId = g.Key.TechnologyId,
                    PowerId = g.Key.PowerId,
                    HardnessId = g.Key.HardnessId,
                    PowerName = first.PowerName,
                    HardnessName = first.HardnessName,
                    IsLongwallMaterialUnitPriceCGH = g.Key.IsLongwallMaterialUnitPriceCGH,
                    TechnologyName = first.TechnologyName,
                    LongwallParameters = first.LongwallParameters,
                    CuttingThickness = first.CuttingThickness,
                    SeamFaceName = first.SeamFaceName,
                    Periods = g.OrderByDescending(p => p.StartMonth)
                               .Select(p =>
                               {
                                   var baseCost = costTotalsByEntityId.GetValueOrDefault(p.Id, 0);
                                   return new LongwallMaterialUnitPricePeriodDto
                                   {
                                       Id = p.Id,
                                       StartMonth = p.StartMonth,
                                       EndMonth = p.EndMonth,
                                       TotalPrice = baseCost * (1 + (p.OtherMaterialValue / 100d))
                                   };
                               })
                               .ToList()
                };
            })
            .OrderByCodeNatural(x => x.Code)
            .ThenBy(x => x.ProcessName)
            .ToList();

        var totalCount = grouped.Count;
        var pagedData = request.IgnorePagination
            ? grouped
            : grouped.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

        var result = new PaginationResponse<GroupedLongwallMaterialUnitPriceDto>(
            pagedData,
            totalCount,
            pageNumber,
            pageSize);

        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }

    private sealed class LongwallMaterialUnitPriceBaseData
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public Guid ProcessId { get; init; }
        public string ProcessName { get; init; } = string.Empty;
        public Guid LongwallParametersId { get; init; }
        public Guid CuttingThicknessId { get; init; }
        public Guid SeamFaceId { get; init; }
        public Guid? TechnologyId { get; init; }
        public Guid? PowerId { get; init; }
        public Guid? HardnessId { get; init; }
        public string PowerName { get; init; } = string.Empty;
        public string HardnessName { get; init; } = string.Empty;
        public bool IsLongwallMaterialUnitPriceCGH { get; init; }
        public string TechnologyName { get; init; } = string.Empty;
        public LongwallParametersDto LongwallParameters { get; init; } = new();
        public CuttingThicknessDto CuttingThickness { get; init; } = new();
        public string SeamFaceName { get; init; } = string.Empty;
        public DateOnly StartMonth { get; init; }
        public DateOnly EndMonth { get; init; }
        public double OtherMaterialValue { get; init; }
    }
}
