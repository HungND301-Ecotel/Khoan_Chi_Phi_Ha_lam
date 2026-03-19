using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Queries;

public record ExportExcelPartQuery() : IRequest<byte[]>;

public class ExportExcelPartQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelPartQuery, byte[]>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Equipment> _equipmentRepository = unitOfWork.GetRepository<Domain.Entities.Index.Equipment>();

    public async Task<byte[]> Handle(ExportExcelPartQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string> { nameof(PartExcelDto.Id) };

        var list = await _partRepository.GetAllAsync(
            include: p => p
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.EquipmentParts).ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
            .Include(p => p.Code)
            .Include(p => p.Costs),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var equipments = await _equipmentRepository.GetAllAsync(
            selector: u => u.Code.Value,
            include: u => u.Include(u => u.Code),
            disableTracking: true);
        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(PartExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
            { nameof(PartExcelDto.EquipmentCodes), equipments.ToList() },
        };

        var dtoList = list.Select(l => new PartExcelDto
        {
            Id = l.Id,
            Code = l.Code.Value,
            Name = l.Name,
            UnitOfMeasureName = l.UnitOfMeasure?.Name ?? string.Empty,
            EquipmentCodes = string.Join(", ", l.EquipmentParts
                .Where(e => e.Equipment?.Code != null)
                .Select(e => e.Equipment!.Code!.Value)
                .OrderBy(code => code)),
            Cost = costService.BuildExcelCostString(l.Costs.ToList())
        });

        return excelService.ExportToExcel(dtoList, "Phụ tùng", listHiddenProperty, dropdownConfigs);
    }
}
