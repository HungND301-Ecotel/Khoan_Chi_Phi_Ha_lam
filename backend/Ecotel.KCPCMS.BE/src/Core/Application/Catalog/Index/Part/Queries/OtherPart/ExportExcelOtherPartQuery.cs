using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Queries.Part;

public record ExportExcelOtherPartQuery() : IRequest<byte[]>;

public class ExportExcelOtherPartQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelOtherPartQuery, byte[]>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Equipment> _equipmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Equipment>();

    public async Task<byte[]> Handle(ExportExcelOtherPartQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string> { nameof(OtherPartExcelDto.Id) };

        var list = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.OtherPart,
            include: p => p
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
            .Include(p => p.Code)
            .Include(p => p.Costs),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(OtherPartExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
        };

        var dtoList = list.Select(l => new OtherPartExcelDto
        {
            Id = l.Id,
            Code = l.Code.Value,
            Name = l.Name,
            UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,            Cost = costService.BuildExcelCostString(l.Costs.ToList())
        });

        return excelService.ExportToExcel(dtoList, "Phụ tùng khác", listHiddenProperty, dropdownConfigs);
    }
}

