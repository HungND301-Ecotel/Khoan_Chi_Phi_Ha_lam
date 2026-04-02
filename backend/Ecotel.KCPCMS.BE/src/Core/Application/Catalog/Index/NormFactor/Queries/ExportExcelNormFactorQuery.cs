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

    public async Task<byte[]> Handle(ExportExcelNormFactorQuery request, CancellationToken cancellationToken)
    {
        var hiddenProperties = new List<string> { nameof(NormFactorExcelDto.Id) };

        var normFactors = await _normFactorRepository.GetAllAsync(
            include: q => q
                .Include(n => n.ProductionProcess).ThenInclude(p => p.Code)
                .Include(n => n.Hardness)
                .Include(n => n.StoneClampRatio)
                .Include(n => n.TargetHardness)
                .Include(n => n.NormFactorAssignmentCodes).ThenInclude(nfa => nfa.AssignmentCode).ThenInclude(a => a.Code),
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
                    .Prepend("Không có")
                    .ToList()
            },
            {
                nameof(NormFactorExcelDto.SteelMeshTypeName),
                new List<string>
                {
                    GetSteelMeshDisplayName(SteelMeshType.None),
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
            }
        };

        var dtoList = normFactors.Select(n => new NormFactorExcelDto
        {
            Id = n.Id,
            ProductionProcessName = n.ProductionProcess?.Code != null
                ? $"{n.ProductionProcess.Code.Value} - {n.ProductionProcess.Name}"
                : string.Empty,
            HardnessName = n.Hardness?.Value ?? "Không có",
            SteelMeshTypeName = GetSteelMeshDisplayName(n.SteelMeshType),
            StoneClampRatioName = n.StoneClampRatio?.Value ?? string.Empty,
            AffectAssignmentCodes = string.Join(", ", n.NormFactorAssignmentCodes
                .Where(a => a.AssignmentCode?.Code != null)
                .Select(a => a.AssignmentCode.Code!.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v)),
            Value = n.Value,
            TargetHardnessName = n.TargetHardness?.Value ?? "Định mức hiện tại"
        });

        return excelService.ExportToExcel(dtoList, "Hệ số định mức", hiddenProperties, dropdownConfigs);
    }

    private static string GetSteelMeshDisplayName(SteelMeshType steelMeshType)
    {
        return steelMeshType switch
        {
            SteelMeshType.SingleLayerSteelMesh => "Trải 1 lớp lưới thép",
            SteelMeshType.DoubleLayerSteelMesh => "Trải 2 lớp lưới thép",
            _ => "Không áp dụng"
        };
    }
}
