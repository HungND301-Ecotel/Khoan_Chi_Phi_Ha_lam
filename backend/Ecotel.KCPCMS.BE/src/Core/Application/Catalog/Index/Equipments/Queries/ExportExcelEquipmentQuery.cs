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
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();

    public async Task<byte[]> Handle(ExportExcelEquipmentQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(EquipmentExcelDto.Id));

        var list = await _equipmentRepository.GetAllAsync(
            include: p => p
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Costs)
            .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Part).ThenInclude(part => part.Code)
            .Include(p => p.Code!),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var partDropdown = (await _partRepository.GetAllAsync(
                predicate: p => p.Type == Domain.Common.Enums.PartType.Part,
                selector: p => p.Code!.Value,
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(EquipmentExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
            { nameof(EquipmentExcelDto.PartCode), partDropdown }
        };

        var dtoList = new List<EquipmentExcelDto>();
        foreach (var l in list.OrderBy(x => x.Code!.Value).ThenBy(x => x.Name))
        {
            var partCodes = l.EquipmentParts
                .Where(ep => ep.Part?.Code != null)
                .Select(ep => ep.Part!.Code!.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code)
                .ToList();

            if (!partCodes.Any())
            {
                dtoList.Add(new EquipmentExcelDto
                {
                    Id = l.Id,
                    Code = l.Code?.Value ?? string.Empty,
                    Name = l.Name,
                    PartCode = string.Empty,
                    UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,
                    Cost = costService.BuildExcelCostString(l.Costs.ToList())
                });
                continue;
            }

            for (var i = 0; i < partCodes.Count; i++)
            {
                dtoList.Add(new EquipmentExcelDto
                {
                    Id = i == 0 ? l.Id : Guid.Empty,
                    Code = i == 0 ? (l.Code?.Value ?? string.Empty) : string.Empty,
                    Name = i == 0 ? l.Name : string.Empty,
                    PartCode = partCodes[i],
                    UnitOfMeasureName = i == 0 ? (l.UnitOfMeasure?.Name ?? string.Empty) : string.Empty,
                    Cost = i == 0 ? costService.BuildExcelCostString(l.Costs.ToList()) : string.Empty
                });
            }
        }

        return excelService.ExportToExcel(dtoList, "Thiết bị", listHiddenProperty, dropdownConfigs);
    }
}
