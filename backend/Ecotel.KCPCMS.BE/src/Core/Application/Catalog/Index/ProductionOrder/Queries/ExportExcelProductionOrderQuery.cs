using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOrder;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.ProductionOrder.Queries;

public record ExportExcelProductionOrderQuery() : IRequest<byte[]>;

public class ExportExcelProductionOrderQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelProductionOrderQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionOrder>();

    public async Task<byte[]> Handle(ExportExcelProductionOrderQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string> { nameof(ProductionOrderExcelDto.Id) };

        var list = await _productionOrderRepository.GetAllAsync(
            include: p => p.Include(p => p.Code),
            disableTracking: true);

        var dtoList = list.Select(p => new ProductionOrderExcelDto
        {
            Id = p.Id,
            StartMonth = p.StartMonth,
            EndMonth = p.EndMonth,
            Code = p.Code.Value,
            Name = p.Name
        });

        return excelService.ExportToExcel(dtoList, "Quyết định, lệnh sản xuất", listHiddenProperty);
    }
}
