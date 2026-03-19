using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelProductionOrderQuery() : IRequest<byte[]>;

public class ExportExcelProductionOrderQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelProductionOrderQuery, byte[]>
{
    private readonly IWriteRepository<ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<ProductionOrder>();
    public async Task<byte[]> Handle(ExportExcelProductionOrderQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(InsertItemExcelDto.Id));

        var list = await _productionOrderRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<InsertItemExcelDto>>(), "Quyết định, lệnh sản xuất", listHiddenProperty);
    }
}