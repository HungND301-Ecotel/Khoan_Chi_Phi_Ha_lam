using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.NormFactor;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record ExportExcelNormFactorQuery() : IRequest<byte[]>;

public class ExportExcelNormFactorQueryHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelNormFactorQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Index.NormFactor> _normFactorRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.NormFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Material>();

    public async Task<byte[]> Handle(ExportExcelNormFactorQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(NormFactorExcelDto.Id) };

        var normFactors = await _normFactorRepository.GetAllAsync(
            include: q => q
                .Include(n => n.ProductionProcess).ThenInclude(p => p.Code)
                .Include(n => n.Hardness)
                .Include(n => n.StoneClampRatio)
                .Include(n => n.NormFactorAssignmentCodes).ThenInclude(nfa => nfa.AssignmentCode).ThenInclude(a => a.Code)
                .Include(n => n.NormFactorAssignmentCodes).ThenInclude(nfa => nfa.Material).ThenInclude(m => m.Code)
                .Include(n => n.NormFactorAssignmentCodes).ThenInclude(nfa => nfa.TargetHardness),
            disableTracking: true);

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            include: q => q.Include(p => p.Code),
            disableTracking: true);

        var hardnesses = await _hardnessRepository.GetAllAsync(
            selector: h => h.Value,
            disableTracking: true);

        var stoneClampRatios = await _stoneClampRatioRepository.GetAllAsync(
            selector: s => s.Value,
            disableTracking: true);

        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(
            include: q => q.Include(a => a.Code),
            disableTracking: true);

        var materials = await _materialRepository.GetAllAsync(
            include: q => q.Include(m => m.Code),
            disableTracking: true);

        var dropdownConfigs = new Dictionary<string, List<string>>
        {
            {
                nameof(NormFactorExcelDto.ProductionProcessName),
                productionProcesses
                    .Where(p => p.Code != null)
                    .Select(p => $"{p.Code!.Value} - {p.Name}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.HardnessName),
                hardnesses
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.SteelMeshTypeName),
                new List<string>
                {
                    GetSteelMeshDisplayName(SteelMeshType.SingleLayerSteelMesh),
                    GetSteelMeshDisplayName(SteelMeshType.DoubleLayerSteelMesh)
                }
            },
            {
                nameof(NormFactorExcelDto.StoneClampRatioName),
                stoneClampRatios
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.TargetHardnessName),
                hardnesses
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .Prepend("Định mức hiện tại")
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.AssignmentCode),
                assignmentCodes
                    .Where(a => a.Code != null && !string.IsNullOrWhiteSpace(a.Code!.Value))
                    .Select(a => a.Code!.Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.MaterialCode),
                materials
                    .Where(m => m.Code != null && !string.IsNullOrWhiteSpace(m.Code!.Value))
                    .Select(m => $"{m.Code!.Value.Trim()} - {m.Name}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v)
                    .ToList()
            }
        };

        var dtoList = normFactors.SelectMany(n =>
        {
            var baseDto = new NormFactorExcelDto
            {
                Id = n.Id,
                ProductionProcessName = n.ProductionProcess?.Code != null
                    ? $"{n.ProductionProcess.Code.Value} - {n.ProductionProcess.Name}"
                    : string.Empty,
                HardnessName = n.Hardness?.Value ?? string.Empty,
                SteelMeshTypeName = GetSteelMeshDisplayName(n.SteelMeshType),
                StoneClampRatioName = n.StoneClampRatio?.Value ?? string.Empty
            };

            if (!n.NormFactorAssignmentCodes.Any())
            {
                return new List<NormFactorExcelDto>
                {
                    new()
                    {
                        Id = baseDto.Id,
                        ProductionProcessName = baseDto.ProductionProcessName,
                        HardnessName = baseDto.HardnessName,
                        SteelMeshTypeName = baseDto.SteelMeshTypeName,
                        StoneClampRatioName = baseDto.StoneClampRatioName,
                        AssignmentCode = string.Empty,
                        MaterialCode = string.Empty,
                        Value = 0,
                        TargetHardnessName = "Định mức hiện tại"
                    }
                };
            }

            return n.NormFactorAssignmentCodes
                .OrderBy(a => a.AssignmentCode?.Code?.Value)
                .ThenBy(a => a.Material?.Code?.Value)
                .Select(a => new NormFactorExcelDto
                {
                    Id = baseDto.Id,
                    ProductionProcessName = baseDto.ProductionProcessName,
                    HardnessName = baseDto.HardnessName,
                    SteelMeshTypeName = baseDto.SteelMeshTypeName,
                    StoneClampRatioName = baseDto.StoneClampRatioName,
                    AssignmentCode = a.AssignmentCode?.Code?.Value ?? string.Empty,

                    MaterialCode = a.Material?.Code?.Value != null
                        ? $"{a.Material.Code.Value} - {a.Material.Name}"
                        : string.Empty,

                    Value = a.Value,
                    TargetHardnessName = a.TargetHardness?.Value ?? "Định mức hiện tại"
                })
                .ToList();
        });

        return excelService.ExportToExcel(dtoList, "Hệ số định mức", hiddenProperties, dropdownConfigs);
    }

    private static string GetSteelMeshDisplayName(SteelMeshType steelMeshType)
    {
        return steelMeshType switch
        {
            SteelMeshType.SingleLayerSteelMesh => "Trải 1 lớp lưới thép",
            SteelMeshType.DoubleLayerSteelMesh => "Trải 2 lớp lưới thép",
            _ => string.Empty
        };
    }
}