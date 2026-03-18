using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;

namespace Application.Catalog.Index.Metrics.Queries;

public record ExportExcelInsertItemQuery() : IRequest<byte[]>;

public class ExportExcelInsertItemQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelInsertItemQuery, byte[]>
{
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    public async Task<byte[]> Handle(ExportExcelInsertItemQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(InsertItemExcelDto.Id));

        var list = await _insertItemRepository.GetAllAsync(disableTracking: true);

        return excelService.ExportToExcel(list.Adapt<List<InsertItemExcelDto>>(), "Chèn", listHiddenProperty);
    }
}