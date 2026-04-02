using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MaterialEntity = Domain.Entities.Index.Material;

namespace Application.Catalog.Index.Material.Queries;

public record ExportExcelMaterialQuery(MaterialType MaterialType = MaterialType.MaterialInContract) : IRequest<byte[]>;

public class ExportExcelMaterialQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService) : IRequestHandler<ExportExcelMaterialQuery, byte[]>
{
    private readonly IWriteRepository<MaterialEntity> _materialEntityRepository = unitOfWork.GetRepository<MaterialEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();

    public async Task<byte[]> Handle(ExportExcelMaterialQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string>();
        listHiddenProperty.Add(nameof(MaterialExcelDto.Id));

        var list = await _materialEntityRepository.GetAllAsync(
            predicate: p => p.MaterialType == request.MaterialType,
            include: p => p
            .Include(p => p.AssignmentCode).ThenInclude(a => a.Code)
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.Costs)
            .Include(p => p.Code!),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(selector: u => u.Code!.Value, include: a => a.Include(a => a.Code!), disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(MaterialExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() },
            { nameof(MaterialExcelDto.AssignmentCode), assignmentCodes.ToList() },
        };
        var dtoList = list.Select(l =>
        {
            return new MaterialExcelDto
            {
                Id = l.Id,
                Code = l.Code?.Value ?? "",
                Name = l.Name,
                UnitOfMeasureName = l.UnitOfMeasure?.Name ?? "",
                AssignmentCode = l.AssignmentCode?.Code?.Value ?? "",
                Cost = costService.BuildExcelCostString(l.Costs.ToList()),
            };
        });

        return excelService.ExportToExcel(dtoList, "Thiết bị", listHiddenProperty, dropdownConfigs);
    }
}
