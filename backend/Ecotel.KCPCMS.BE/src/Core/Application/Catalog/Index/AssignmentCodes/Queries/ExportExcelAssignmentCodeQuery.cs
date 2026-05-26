using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AssignmentCodes.Queries;

public record ExportExcelAssignmentCodeQuery() : IRequest<byte[]>;

public class ExportExcelAssignmentCodeQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelAssignmentCodeQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<Domain.Entities.Index.UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();

    public async Task<byte[]> Handle(ExportExcelAssignmentCodeQuery request, CancellationToken cancellationToken)
    {
        var listHiddenProperty = new List<string> { nameof(AssignmentCodeExcelDto.Id) };

        var list = await _assignmentCodeRepository.GetAllAsync(
            include: s => s
                .Include(s => s.UnitOfMeasure)
                .Include(s => s.Code)
                .Include(s => s.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Code),
            disableTracking: true);

        var unitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(selector: u => u.Name, disableTracking: true);
        var materialCodes = (await _materialRepository.GetAllAsync(
                selector: m => m.Code != null ? m.Code.Value : string.Empty,
                include: m => m.Include(x => x.Code),
                disableTracking: true))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code)
            .ToList();

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            { nameof(AssignmentCodeExcelDto.MaterialCode), materialCodes },
            { nameof(AssignmentCodeExcelDto.MaterialRole), GetMaterialRoleOptions() },
            { nameof(AssignmentCodeExcelDto.UnitOfMeasureName), unitOfMeasures.ToList() }
        };

        var dtoList = new List<AssignmentCodeExcelDto>();
        foreach (var item in list.OrderBy(x => x.Code!.Value).ThenBy(x => x.Name))
        {
            var linkedMaterials = item.AssignmentCodeMaterials
                .Where(m => m.Material?.Code != null)
                .Select(m => new
                {
                    MaterialCode = m.Material!.Code!.Value,
                    MaterialRole = ToDisplayRole(m.Role),
                })
                .OrderBy(m => m.MaterialCode)
                .ThenBy(m => m.MaterialRole)
                .ToList();

            if (!linkedMaterials.Any())
            {
                dtoList.Add(new AssignmentCodeExcelDto
                {
                    Id = item.Id,
                    Code = item.Code?.Value ?? string.Empty,
                    Name = item.Name,
                    MaterialCode = string.Empty,
                    MaterialRole = string.Empty,
                    UnitOfMeasureName = item.UnitOfMeasure?.Name ?? string.Empty
                });
                continue;
            }

            for (var i = 0; i < linkedMaterials.Count; i++)
            {
                dtoList.Add(new AssignmentCodeExcelDto
                {
                    Id = i == 0 ? item.Id : Guid.Empty,
                    Code = i == 0 ? (item.Code?.Value ?? string.Empty) : string.Empty,
                    Name = i == 0 ? item.Name : string.Empty,
                    MaterialCode = linkedMaterials[i].MaterialCode,
                    MaterialRole = linkedMaterials[i].MaterialRole,
                    UnitOfMeasureName = i == 0 ? (item.UnitOfMeasure?.Name ?? string.Empty) : string.Empty
                });
            }
        }

        return excelService.ExportToExcel(
            data: dtoList,
            sheetName: "Nhóm vật tư, tài sản",
            hiddenProperties: listHiddenProperty,
            dropdownData: dropdownConfigs);
    }

    private static List<string> GetMaterialRoleOptions()
    {
        return
        [
            ToDisplayRole(AssignmentCodeMaterialRole.Material),
            ToDisplayRole(AssignmentCodeMaterialRole.OtherMaterial),
        ];
    }

    private static string ToDisplayRole(AssignmentCodeMaterialRole role)
    {
        return role == AssignmentCodeMaterialRole.OtherMaterial
            ? "Vật tư, tài sản khác"
            : "Vật tư, tài sản";
    }
}
