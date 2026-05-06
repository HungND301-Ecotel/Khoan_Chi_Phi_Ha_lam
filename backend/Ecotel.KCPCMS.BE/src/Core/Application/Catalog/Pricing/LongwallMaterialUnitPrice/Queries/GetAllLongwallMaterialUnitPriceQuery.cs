using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record class GetAllLongwallMaterialUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<LongwallMaterialUnitPriceDto>>;

public class GetAllLongwallMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<GetAllLongwallMaterialUnitPriceQuery, PaginationResponse<LongwallMaterialUnitPriceDto>>
{
    private const string CacheSignalKey = "LongwallMaterialUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();

    public async Task<PaginationResponse<LongwallMaterialUnitPriceDto>> Handle(GetAllLongwallMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageIndex > 0 ? request.PageIndex : 1;
        var pageSize = request.PageSize == 0 ? int.MaxValue : request.PageSize > 0 ? request.PageSize : 10;
        var searchTerm = request.Search?.Trim();
        var normalizedSearchTerm = searchTerm?.ToLower();
        var cacheKey = $"{CacheSignalKey}:All:{pageNumber}:{pageSize}:{normalizedSearchTerm ?? "empty"}:{request.IgnorePagination}";

        var cachedResult = await cacheService.GetAsync<PaginationResponse<LongwallMaterialUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var baseQuery = _materialUnitPriceRepository.GetAll()
            .Where(m => string.IsNullOrWhiteSpace(normalizedSearchTerm) ||
                        m.Code != null && m.Code.Value.ToLower().Contains(normalizedSearchTerm));

        var totalCount = await baseQuery.CountAsync(cancellationToken);

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

        var orderedBaseData = allBaseData
            .OrderByCodeNatural(m => m.Code)
            .ThenBy(m => m.ProcessName);

        var pageData = request.IgnorePagination
            ? orderedBaseData.ToList()
            : orderedBaseData
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

        if (pageData.Count == 0)
        {
            return new PaginationResponse<LongwallMaterialUnitPriceDto>([], totalCount, pageNumber, pageSize);
        }

        var selectedIds = pageData.Select(m => m.Id).ToList();
        var costs = await _materialUnitPriceRepository.GetAll()
            .Where(m => selectedIds.Contains(m.Id))
            .SelectMany(m => m.MaterialUnitPriceAssignmentCodes.Select(cost => new LongwallMaterialUnitPriceCostData
            {
                MaterialUnitPriceId = m.Id,
                AssignmentCodeId = cost.AssignmentCodeId,
                TotalPrice = cost.TotalPrice
            }))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var costsByMaterialUnitPriceId = costs
            .GroupBy(cost => cost.MaterialUnitPriceId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(cost => new MaterialUnitPriceAssignmentCodeDto
                    {
                        AssignmentCodeId = cost.AssignmentCodeId,
                        TotalPrice = cost.TotalPrice
                    })
                    .ToList());

        var result = pageData.Select(m =>
        {
            var materialCosts = costsByMaterialUnitPriceId.GetValueOrDefault(m.Id) ?? [];

            return new LongwallMaterialUnitPriceDto
            {
                Id = m.Id,
                Code = m.Code,
                ProcessId = m.ProcessId,
                ProcessName = m.ProcessName,
                LongwallParametersId = m.LongwallParametersId,
                CuttingThicknessId = m.CuttingThicknessId,
                SeamFaceId = m.SeamFaceId,
                TechnologyId = m.TechnologyId,
                PowerId = m.PowerId,
                HardnessId = m.HardnessId,
                PowerName = m.PowerName,
                HardnessName = m.HardnessName,
                IsLongwallMaterialUnitPriceCGH = m.IsLongwallMaterialUnitPriceCGH,
                TechnologyName = m.TechnologyName,
                LongwallParameters = m.LongwallParameters,
                CuttingThickness = m.CuttingThickness,
                SeamFaceName = m.SeamFaceName,
                StartMonth = m.StartMonth,
                EndMonth = m.EndMonth,
                OtherMaterialValue = m.OtherMaterialValue,
                TotalPrice = materialCosts.Sum(cost => cost.TotalPrice) + m.OtherMaterialValue,
                Costs = materialCosts
            };
        }).ToList();

        var paginationResponse = new PaginationResponse<LongwallMaterialUnitPriceDto>(result, totalCount, pageNumber, pageSize);
        cacheService.SetWithSignal(cacheKey, paginationResponse, CacheSignalKey);

        return paginationResponse;
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

    private sealed class LongwallMaterialUnitPriceCostData
    {
        public Guid MaterialUnitPriceId { get; init; }
        public Guid AssignmentCodeId { get; init; }
        public double TotalPrice { get; init; }
    }
}
