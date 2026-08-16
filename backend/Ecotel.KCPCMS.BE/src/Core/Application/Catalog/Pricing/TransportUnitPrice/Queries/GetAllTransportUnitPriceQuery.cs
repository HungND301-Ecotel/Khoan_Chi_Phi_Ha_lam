using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Queries;

public record GetAllTransportUnitPriceQuery(int PageIndex, int PageSize, string? Search, bool IgnorePagination) : IRequest<PaginationResponse<TransportUnitPriceGroupDto>>;

// Danh sách phân trang gộp theo (Công đoạn sản xuất, Thời gian) — đúng 1 hàng = 1 lần tạo mới
// trên form (nhiều dòng Tuyến/Đơn vị/Nhóm vật tư/Chất lượng thiết bị con nằm trong Items).
// TransportUnitPrice không có cột batch/group riêng nên phải gộp tại query time (giống cách
// GetAllTransportPlanLineQuery gộp theo Department/Month).
public class GetAllTransportUnitPriceQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllTransportUnitPriceQuery, PaginationResponse<TransportUnitPriceGroupDto>>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _repository = unitOfWork.GetRepository<TransportUnitPriceEntity>();

    public async Task<PaginationResponse<TransportUnitPriceGroupDto>> Handle(GetAllTransportUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var searchTerm = (request.Search ?? string.Empty).Trim().ToLower();

        var items = await _repository.GetAll()
            .Where(t => string.IsNullOrWhiteSpace(searchTerm) ||
                        (t.ProductionProcess != null && t.ProductionProcess.Name.ToLower().Contains(searchTerm)) ||
                        (t.TransportRoute != null && t.TransportRoute.Name.ToLower().Contains(searchTerm)) ||
                        (t.Equipment != null && t.Equipment.Name.ToLower().Contains(searchTerm)))
            .Include(t => t.ProductionProcess)
                .ThenInclude(p => p!.Code)
            .Include(t => t.TransportRoute)
            .Include(t => t.Department)
            .Include(t => t.Equipment)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var groups = items
            .GroupBy(t => new { t.ProductionProcessId, t.StartMonth, t.EndMonth })
            .Select(g =>
            {
                var orderedItems = g
                    .OrderBy(x => x.EquipmentQuality)
                    .ThenBy(x => x.TransportRouteId)
                    .ToList();
                var first = orderedItems[0];

                return new TransportUnitPriceGroupDto
                {
                    Id = first.Id,
                    ProductionProcessId = g.Key.ProductionProcessId,
                    ProductionProcessCode = first.ProductionProcess?.Code?.Value,
                    ProductionProcessName = first.ProductionProcess?.Name,
                    StartMonth = g.Key.StartMonth,
                    EndMonth = g.Key.EndMonth,
                    ItemCount = orderedItems.Count,
                    Items = orderedItems.Select(ToItemDto).ToList(),
                };
            })
            .OrderByDescending(x => x.StartMonth)
            .ThenBy(x => x.ProductionProcessCode)
            .ToList();

        var totalCount = groups.Count;

        var pagedGroups = request.IgnorePagination
            ? groups
            : groups.Skip((request.PageIndex - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new PaginationResponse<TransportUnitPriceGroupDto>(pagedGroups, totalCount, request.PageIndex, request.PageSize);
    }

    private static TransportUnitPriceDto ToItemDto(TransportUnitPriceEntity t) => new()
    {
        Id = t.Id,
        ProductionProcessId = t.ProductionProcessId,
        ProductionProcessName = t.ProductionProcess?.Name,
        TransportRouteId = t.TransportRouteId,
        TransportRouteName = t.TransportRoute?.Name,
        DepartmentId = t.DepartmentId,
        DepartmentName = t.Department?.Name,
        EquipmentId = t.EquipmentId,
        EquipmentName = t.Equipment?.Name,
        EquipmentQuality = t.EquipmentQuality,
        MaterialFuelUnitPrice = t.MaterialFuelUnitPrice,
        PowerUnitPrice = t.PowerUnitPrice,
        MaintenanceUnitPrice = t.MaintenanceUnitPrice,
        Quantity = t.Quantity,
        IsLowVolumeCase = t.IsLowVolumeCase,
        StartMonth = t.StartMonth,
        EndMonth = t.EndMonth,
    };
}
