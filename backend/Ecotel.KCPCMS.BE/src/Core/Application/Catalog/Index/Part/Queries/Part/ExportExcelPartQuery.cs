using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Queries.Part;

public record ExportExcelPartQuery() : IRequest<byte[]>;

public class ExportExcelPartQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelPartQuery, byte[]>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();

    public async Task<byte[]> Handle(ExportExcelPartQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();

        var list = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.Part,
            include: p => p
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
            .Include(p => p.PartProcessGroups).ThenInclude(ppg => ppg.ProcessGroup).ThenInclude(pg => pg.Code)
            .Include(p => p.Code)
            .Include(p => p.Costs),
            disableTracking: true);

        var equipments = await _equipmentRepository.GetAllAsync(
            selector: e => $"{e.Code.Value} - {e.Name}",
            include: e => e.Include(e => e.Code),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(PartExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
            { nameof(PartExcelDto.EquipmentCode), equipments.ToList() },
        };

        var dtoList = list.SelectMany(l =>
        {
            var cost = costService.BuildExcelCostString(l.Costs.ToList());
            var processGroupCodes = string.Join(", ", l.PartProcessGroups
                .Where(ppg => ppg.ProcessGroup?.Code != null)
                .Select(ppg => ppg.ProcessGroup!.Code!.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code));
            var equipmentRows = l.EquipmentParts
                .Where(ep => ep.Equipment?.Code != null)
                .Select(ep => new PartExcelDto
                {
                    EquipmentCode = $"{ep.Equipment!.Code!.Value} - {ep.Equipment.Name}",
                    ProcessGroupCodes = processGroupCodes,
                    Code = l.Code.Value,
                    Name = l.Name,
                    UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,
                    ReplacementTimeStandard = l.ReplacementTimeStandard,
                    Cost = cost
                })
                .ToList();

            if (equipmentRows.Any())
            {
                return equipmentRows;
            }

            return new List<PartExcelDto>
            {
                new()
                {
                    EquipmentCode = string.Empty,
                    ProcessGroupCodes = processGroupCodes,
                    Code = l.Code.Value,
                    Name = l.Name,
                    UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,
                    ReplacementTimeStandard = l.ReplacementTimeStandard,
                    Cost = cost
                }
            };
        });

        return excelService.ExportToExcel(dtoList.OrderBy(d => d.EquipmentCode).ThenBy(d => d.Code).ThenBy(d => d.Name), "Phụ tùng", listHiddenProperty, dropdownConfigs);
    }
}
