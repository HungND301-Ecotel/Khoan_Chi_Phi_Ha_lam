using Application.Common.Caching;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Queries;

public record GetAllLowValuePerishableSupplyUnitPriceQuery(
    int PageIndex,
    int PageSize,
    string? Search,
    bool IgnorePagination,
    LowValuePerishableSupplyType Type) : IRequest<PaginationResponse<LowValuePerishableSupplyUnitPriceDto>>;

public class GetAllLowValuePerishableSupplyUnitPriceQueryHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService)
    : IRequestHandler<GetAllLowValuePerishableSupplyUnitPriceQuery, PaginationResponse<LowValuePerishableSupplyUnitPriceDto>>
{
    private const string CacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();

    public async Task<PaginationResponse<LowValuePerishableSupplyUnitPriceDto>> Handle(GetAllLowValuePerishableSupplyUnitPriceQuery request, CancellationToken cancellationToken)
    {
        string cacheKey = $"GetAllLowValuePerishableSupplyUnitPrice:{request.PageIndex}:{request.PageSize}:{request.Search ?? "empty"}:{request.IgnorePagination}:{request.Type}";
        PaginationResponse<LowValuePerishableSupplyUnitPriceDto>? cachedResult = await cacheService.GetAsync<PaginationResponse<LowValuePerishableSupplyUnitPriceDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        PaginationFilter filter = new()
        {
            PageNumber = request.PageIndex,
            PageSize = request.PageSize,
            IgnorePagination = request.IgnorePagination,
        };

        IQueryable<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> query = _repository.GetAll()
            .Where(e => e.Type == request.Type)
            .Include(e => e.Department).ThenInclude(d => d!.Code)
            .Include(e => e.ProcessGroup).ThenInclude(pg => pg!.FixedKey)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(e =>
                (e.Department != null && e.Department.Name.Contains(request.Search)) ||
                (e.Department != null && e.Department.Code != null && e.Department.Code.Value.Contains(request.Search)) ||
                (e.ProcessGroup != null && e.ProcessGroup.Name.Contains(request.Search)) ||
                (e.ProcessGroup != null && e.ProcessGroup.FixedKey != null && e.ProcessGroup.FixedKey.Key.Contains(request.Search)));
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> data = await query.ToListAsync(cancellationToken);
        IEnumerable<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> sortedData = data
            .OrderByCodeNatural(e => e.Department!.Code!.Value)
            .ThenBy(e => e.ProcessGroup!.FixedKey!.Key)
            .ThenByDescending(e => e.StartMonth)
            .ThenByDescending(e => e.EndMonth);

        if (!filter.IgnorePagination)
        {
            sortedData = sortedData
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);
        }

        List<LowValuePerishableSupplyUnitPriceDto> listData = sortedData.Select(e => new LowValuePerishableSupplyUnitPriceDto
        {
            Id = e.Id,
            DepartmentId = e.DepartmentId,
            DepartmentCode = e.Department?.Code?.Value ?? string.Empty,
            DepartmentName = e.Department?.Name ?? string.Empty,
            ProcessGroupId = e.ProcessGroupId,
            ProcessGroupCode = e.ProcessGroup?.FixedKey?.Key ?? string.Empty,
            ProcessGroupName = e.ProcessGroup?.Name ?? string.Empty,
            StartMonth = e.StartMonth,
            EndMonth = e.EndMonth,
            Type = e.Type,
            TotalPrice = e.TotalPrice,
        }).ToList();

        PaginationResponse<LowValuePerishableSupplyUnitPriceDto> result = new(listData, totalCount, filter.PageNumber, filter.PageSize);
        cacheService.SetWithSignal(cacheKey, result, CacheSignalKey);
        return result;
    }
}