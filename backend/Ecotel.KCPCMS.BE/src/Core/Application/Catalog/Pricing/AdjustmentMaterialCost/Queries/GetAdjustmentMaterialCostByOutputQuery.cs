using System.Globalization;
using Application.Catalog.Pricing.Common;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmnetMaterialCost;
using Domain.Common.Enums;
using Domain.Entities.Index;
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
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<AkFactorConfig> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfig>();

    public async Task<AdjustmentMaterialCostDetailDto> Handle(GetAdjustmentMaterialCostByOutputQuery request, CancellationToken cancellationToken)
    {
        // Get planned output by Id
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice).ThenInclude(p => p.Product).ThenInclude(p => p.ProcessGroup)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.ProductUnitPrice).ThenInclude(p => p.Product)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes).ThenInclude(a => a.TargetHardness)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.NormFactor).ThenInclude(n => n.Hardness)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Costs)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Code)
                .Include(o => o.PlannedMaterialCost).ThenInclude(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.AssignmentCode).ThenInclude(a => a.Code)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedOutputNotFound);

        var adjustmentOutputInfo = await _productUnitPriceRepository.GetAll()
            .Where(p => p.ScenarioType == ProductUnitPriceScenarioType.Adjustment &&
                        p.ProductId == plannedOutput.ProductUnitPrice!.ProductId &&
                        p.DepartmentId == plannedOutput.ProductUnitPrice.DepartmentId)
            .SelectMany(p => p.ProductUnitPriceProductionOutputs)
            .Where(p => p.ProductionOutput!.StartMonth == plannedOutput.StartMonth &&
                        p.ProductionOutput.EndMonth == plannedOutput.EndMonth)
            .Select(p => new
            {
                p.ProductionMeters,
                ActualAshContent = p.ProductionOutput!.ProductionOutputProcessGroups
                    .SelectMany(g => g.ProductionOutputProducts)
                    .Where(pp => pp.ProductId == plannedOutput.ProductUnitPrice!.ProductId)
                    .Select(pp => (double?)pp.ActualAshContent)
                    .FirstOrDefault() ?? 0
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (adjustmentOutputInfo == null || adjustmentOutputInfo.ProductionMeters <= 0)
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
        var slideUnitPriceCost = Domain.Entities.Pricing.PlannedMaterialCost.RoundUnitPrice(
            plannedMaterialCost.SlideUnitPriceAssignmentCode?.Amount ?? 0);

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
                    MaterialCost = Domain.Entities.Pricing.PlannedMaterialCost.RoundUnitPrice(originalAmount),
                    MaterialUnitPriceCost = Domain.Entities.Pricing.PlannedMaterialCost.RoundUnitPrice(originalAmount),
                    TotalPrice = Domain.Entities.Pricing.PlannedMaterialCost.RoundLineTotal(
                        Domain.Entities.Pricing.PlannedMaterialCost.RoundUnitPrice(originalAmount) * coefficientValue),
                }]
            });
        }

        var dependencies = await PlannedMaterialCostCalculationDependencyLoader.LoadAsync(
            new List<Domain.Entities.Pricing.PlannedMaterialCost> { plannedMaterialCost },
            _tunnelMaterialUnitPriceRepository,
            _lowValuePerishableSupplyUnitPriceRepository,
            cancellationToken);
        var calculationResults = PlannedMaterialCostCalculator.CalculateResultsByCostId(
            new List<Domain.Entities.Pricing.PlannedMaterialCost> { plannedMaterialCost },
            dependencies.TunnelMaterialUnitPrices,
            dependencies.LowValuePerishableSupplyUnitPrices);
        var baseCalculation = calculationResults.GetValueOrDefault(plannedMaterialCost.Id, new PlannedMaterialCostCalculationResult());
        var basePlannedMaterialPrice = baseCalculation.TotalPrice;

        var akConfigs = await _akFactorConfigRepository.GetAll()
            .Where(x => x.ProcessGroupId == plannedOutput.ProductUnitPrice!.Product.ProcessGroupId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var hasAkConfigs = akConfigs.Any();
        var akDiff = hasAkConfigs
            ? (decimal)(plannedOutput.PlanAshContent - adjustmentOutputInfo.ActualAshContent)
            : 0;
        var akRate = hasAkConfigs
            ? AkFactorConfig.ResolveRate(akConfigs, akDiff)
            : 0;
        var akAdjustmentAmount = hasAkConfigs ? akDiff * (decimal)basePlannedMaterialPrice * akRate : 0;
        var adjustedPlannedMaterialPrice = Domain.Entities.Pricing.PlannedMaterialCost.RoundLineTotal(
            basePlannedMaterialPrice + (double)akAdjustmentAmount);

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
            TotalPlannedMaterialPrice = adjustedPlannedMaterialPrice,
            MaterialCost = Domain.Entities.Pricing.PlannedMaterialCost.RoundUnitPrice(materialCost),
            SlideUnitPriceCost = slideUnitPriceCost,
            LowValuePerishableSupplyUnitPriceCost = baseCalculation.LowValuePerishableSupplyUnitPriceCost,
            AkRate = (double)akRate,
            AkRatePercent = (double)akRate * 100,
            NormFactorValue = normFactorValue,
        };
        return result;
    }
}
