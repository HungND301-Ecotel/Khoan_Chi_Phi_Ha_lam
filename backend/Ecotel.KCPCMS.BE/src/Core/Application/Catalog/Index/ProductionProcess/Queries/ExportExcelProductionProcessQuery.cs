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
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();

    public async Task<byte[]> Handle(ExportExcelProductionProcessQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(ProductionProcessExcelDto.Id));

        var list = await _productionProcessRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProcessGroup).ThenInclude(s => s.FixedKey)
                .Include(s => s.Code!).Include(s => s.UnitOfMeasure),
            disableTracking: true);

        var dtoList = list.Select(s => new ProductionProcessExcelDto
        {
            Id = s.Id,
            Code = s.Code?.Value ?? "",
            Name = s.Name,
            ProcessGroupCode = s.ProcessGroup?.FixedKey?.Key ?? "",
            UnitOfMeasureName = s.UnitOfMeasure?.Name ?? ""
        });

        var unitOfMeasureNames = (await _unitOfMeasureRepository.GetAllAsync(
                selector: u => u.Name,
                disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(ProductionProcessExcelDto.UnitOfMeasureName), unitOfMeasureNames }
        };

        return excelService.ExportToExcel(dtoList, "Công đoạn sản xuất", listHiddenProperty, dropdownConfigs);
    }
}