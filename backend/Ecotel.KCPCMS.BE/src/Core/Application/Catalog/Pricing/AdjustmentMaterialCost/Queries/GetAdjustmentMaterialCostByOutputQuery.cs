using System.Globalization;
using Application.Catalog.Pricing.Common;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmnetMaterialCost;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Application.Catalog.Pricing.AdjustmentMaterialCost.Queries;

public record GetAdjustmentMaterialCostByOutputQuery(DefaultIdType Id) : IRequest<AdjustmentMaterialCostDetailDto>;

public class GetAdjustmentMaterialCostByOutputQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdjustmentMaterialCostByOutputQuery, AdjustmentMaterialCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();

    public async Task<AdjustmentMaterialCostDetailDto> Handle(GetAdjustmentMaterialCostByOutputQuery request, CancellationToken cancellationToken)
    {
        // Get planned output by Id
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes).ThenInclude(a => a.TargetHardness)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.Hardness)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Costs)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Code)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.AssignmentCode).ThenInclude(a => a.Code)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedOutputNotFound);

        var adjustmentProductionMeters = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment &&
                        p.ProductId == plannedOutput.ProductUnitPrice!.ProductId)
            .SelectMany(p => p.ProductUnitPriceProductionOutputs)
            .Where(p => p.ProductionOutput!.StartMonth == plannedOutput.StartMonth &&
                        p.ProductionOutput.EndMonth == plannedOutput.EndMonth)
            .Select(p => p.ProductionMeters)
            .FirstOrDefaultAsync(cancellationToken);

        if (adjustmentProductionMeters <= 0)
        {
            throw new ConflictException(CustomResponseMessage.PleaseProvideTheActualOutputProductionMeters);
        }

        var plannedMaterialCost = plannedOutput.PlannedMaterialCost;
        var normFactorAssignment = plannedMaterialCost.NormFactor?.NormFactorAssignmentCodes
            .FirstOrDefault(x => x.AssignmentCodeId == plannedMaterialCost.SlideUnitPriceAssignmentCode?.Material?.AssigmentCodeId)
            ?? plannedMaterialCost.NormFactor?.NormFactorAssignmentCodes.FirstOrDefault();
        var coefficientValue = normFactorAssignment?.Value ?? 1;
        var targetHardnessId = normFactorAssignment?.TargetHardnessId;
        var targetHardnessValue = normFactorAssignment?.TargetHardness?.Value;

        if (plannedMaterialCost.MaterialUnitPrice == null)
        {
            throw new NotFoundException(CustomResponseMessage.MaterialUnitPriceNotFound);
        }

        // Load tunnel materials nếu cần (giống GetPlannedMaterialCostByIdQuery)
        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (targetHardnessId.HasValue
            && plannedMaterialCost.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice currentTunnelMaterial)
        {
            tunnelMaterials = await _tunnelMaterialUnitPriceRepository.GetAll()
                .Where(x => x.HardnessId == targetHardnessId.Value
                    && x.ProcessId == currentTunnelMaterial.ProcessId
                    && x.PassportId == currentTunnelMaterial.PassportId
                    && x.InsertItemId == currentTunnelMaterial.InsertItemId
                    && x.SupportStepId == currentTunnelMaterial.SupportStepId)
                .Include(x => x.MaterialUnitPriceAssignmentCodes)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        // Tính MaterialCost (giống GetPlannedMaterialCostByIdQuery)
        static double SumMaterialUnitPriceCost(MaterialUnitPriceEntity? unitPrice)
        {
            if (unitPrice == null)
            {
                return 0;
            }

            return unitPrice.MaterialUnitPriceAssignmentCodes?.Sum(x => x.TotalPrice) ?? 0;
        }

        TunnelExcavationMaterialUnitPrice? ResolveTargetTunnelMaterialUnitPrice(
            TunnelExcavationMaterialUnitPrice current,
            Guid targetHardnessId,
            IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> candidates)
        {
            var outputStart = plannedOutput.StartMonth;
            var outputEnd = plannedOutput.EndMonth;

            return candidates
                .Where(x =>
                    x.ProcessId == current.ProcessId &&
                    x.PassportId == current.PassportId &&
                    x.InsertItemId == current.InsertItemId &&
                    x.SupportStepId == current.SupportStepId &&
                    x.HardnessId == targetHardnessId &&
                    x.StartMonth <= outputStart &&
                    x.EndMonth >= outputEnd)
                .OrderByDescending(x => x.StartMonth)
                .ThenByDescending(x => x.EndMonth)
                .FirstOrDefault();
        }

        var materialCost = SumMaterialUnitPriceCost(plannedMaterialCost.MaterialUnitPrice) + plannedMaterialCost.MaterialUnitPrice.OtherMaterialvalue;
        if (targetHardnessId.HasValue
            && plannedMaterialCost.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice currentTunnelMaterialForCost)
        {
            var targetMaterial = ResolveTargetTunnelMaterialUnitPrice(
                currentTunnelMaterialForCost,
                targetHardnessId.Value,
                tunnelMaterials);

            if (targetMaterial != null)
            {
                materialCost = SumMaterialUnitPriceCost(targetMaterial) + targetMaterial.OtherMaterialvalue;
            }
        }

        // Tính NormFactorValue
        var normFactorValue = string.Empty;
        if (plannedMaterialCost.NormFactor != null)
        {
            var hardnessValue = targetHardnessValue
                ?? plannedMaterialCost.NormFactor.Hardness?.Value
                ?? string.Empty;

            normFactorValue =
                $"{coefficientValue.ToString(CultureInfo.InvariantCulture)} - {hardnessValue}";
        }

        // SlideUnitPriceCost
        var slideUnitPriceCost = plannedMaterialCost.SlideUnitPriceAssignmentCode?.Amount ?? 0;

        var mCost = new List<AdjustmentMaterialCostAssignmentCode>();

        if (plannedMaterialCost.SlideUnitPriceAssignmentCodeId != null)
        {
            var currentSlide = plannedMaterialCost.SlideUnitPriceAssignmentCode.Material;
            var originalAmount = plannedMaterialCost.SlideUnitPriceAssignmentCode.Amount;
            mCost.Add(new AdjustmentMaterialCostAssignmentCode
            {
                AssignmentCodeId = currentSlide.AssigmentCodeId,
                AssignmentCode = currentSlide.AssignmentCode.Code.Value,
                AssignmentCodeName = currentSlide.AssignmentCode.Name,
                Costs = [new AdjustmentMaterialCostDto
                {
                    MaterialId = currentSlide.Id,
                    MaterialCode = currentSlide?.Code.Value ?? "",
                    MaterialName = currentSlide.Name ?? "",
                    UnitOfMeasureName = currentSlide.UnitOfMeasure?.Name ?? "",
                    OriginalQuantity = 1,
                    CoefficientValue = coefficientValue,
                    FinalQuantity = coefficientValue,
                    MaterialCost = originalAmount,
                    MaterialUnitPriceCost = originalAmount,
                    TotalPrice = originalAmount * coefficientValue,
                }]
            });
        }

        var totalByCostId = PlannedMaterialCostCalculator.CalculateUnitPricesByCostId(
            new List<Domain.Entities.Pricing.PlannedMaterialCost> { plannedMaterialCost },
            tunnelMaterials);

        var result = new AdjustmentMaterialCostDetailDto
        {
            Id = plannedMaterialCost.Id,
            OutputId = plannedOutput.Id,
            MaterialUnitPriceId = plannedMaterialCost.MaterialUnitPriceId,
            ProductUnitPriceId = plannedMaterialCost.ProductUnitPriceId,
            SlideUnitPriceAssignmentCodeId = plannedMaterialCost.SlideUnitPriceAssignmentCodeId,
            NormFactorId = plannedMaterialCost.NormFactorId,
            AdjustmentMaterialCostAssignmentCodes = mCost,
            StoneClampRatioReferenceId = plannedMaterialCost.StoneClampRatioReferenceId,
            TotalPlannedMaterialPrice = totalByCostId.GetValueOrDefault(plannedMaterialCost.Id, 0),
            MaterialCost = materialCost,
            SlideUnitPriceCost = slideUnitPriceCost,
            NormFactorValue = normFactorValue,
        };
        return result;
    }
}
