using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Queries.Part;

public record ExportExcelPartQuery() : IRequest<byte[]>;

public class ExportExcelPartQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelPartQuery, byte[]>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();

    public async Task<byte[]> Handle(ExportExcelPartQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();

        var list = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.Part,
            include: p => p
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Code)
                .Include(p => p.Costs),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(PartExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
        };

        var dtoList = list
            .Select(l => new PartExcelDto
            {
                Code = l.Code?.Value ?? string.Empty,
                Name = l.Name,
                UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,
                ReplacementTimeStandard = l.ReplacementTimeStandard,
                Cost = costService.BuildExcelCostString(l.Costs.ToList())
            })
            .OrderBy(d => d.Code)
            .ThenBy(d => d.Name);

        return excelService.ExportToExcel(dtoList, "Phụ tùng", listHiddenProperty, dropdownConfigs);
    }
}
