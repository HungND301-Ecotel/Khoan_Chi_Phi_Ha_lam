using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Equipments.Queries;

public record ExportExcelEquipmentQuery() : IRequest<byte[]>;

public class ExportExcelEquipmentQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelEquipmentQuery, byte[]>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();

    public async Task<byte[]> Handle(ExportExcelEquipmentQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(EquipmentExcelDto.Id));

        var list = await _equipmentRepository.GetAllAsync(
            include: p => p
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Costs)
            .Include(p => p.EquipmentProcessGroups).ThenInclude(epg => epg.ProcessGroup).ThenInclude(pg => pg.Code)
            .Include(p => p.Code!),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(EquipmentExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() }
        };

        var dtoList = list.Select(l =>
        {
            return new EquipmentExcelDto
            {
                Id = l.Id,
                Code = l.Code?.Value ?? "",
                Name = l.Name,
                ProcessGroupCodes = string.Join(", ", l.EquipmentProcessGroups
                    .Where(epg => epg.ProcessGroup?.Code != null)
                    .Select(epg => epg.ProcessGroup!.Code!.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(code => code)),
                UnitOfMeasureName = l.UnitOfMeasure?.Name ?? "",
                Cost = costService.BuildExcelCostString(l.Costs.ToList())
            };
        });

        return excelService.ExportToExcel(dtoList, "Thiết bị", listHiddenProperty, dropdownConfigs);
    }
}
