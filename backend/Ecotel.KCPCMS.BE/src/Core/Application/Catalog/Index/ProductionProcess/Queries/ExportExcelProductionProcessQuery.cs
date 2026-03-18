using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionProcess;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.ProductionProcess.Queries;

public record ExportExcelProductionProcessQuery() : IRequest<byte[]>;

public class ExportExcelProductionProcessQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelProductionProcessQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    public async Task<byte[]> Handle(ExportExcelProductionProcessQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(ProductionProcessExcelDto.Id));

        var list = await _productionProcessRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProcessGroup).ThenInclude(s => s.Code)
                .Include(s => s.Code!),
            disableTracking: true);

        var dtoList = list.Select(s => new ProductionProcessExcelDto
        {
            Id = s.Id,
            Code = s.Code?.Value ?? "",
            Name = s.Name,
            ProcessGroupCode = s.ProcessGroup?.Code?.Value ?? ""
        });

        return excelService.ExportToExcel(dtoList, "Công đoạn sản xuất", listHiddenProperty);
    }
}