using System.Globalization;
using Application.Catalog.Pricing.Common;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedMaterialCost;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using MaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.MaterialUnitPrice;

namespace Application.Catalog.Pricing.PlannedMaterialCost.Queries;

public record GetPlannedMaterialCostByIdQuery(DefaultIdType Id) : IRequest<PlannedMaterialCostDetailDto>;

public class GetPlannedMaterialCostByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPlannedMaterialCostByIdQuery, PlannedMaterialCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _tunnelMaterialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _lowValuePerishableSupplyUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    public async Task<PlannedMaterialCostDetailDto> Handle(GetPlannedMaterialCostByIdQuery request, CancellationToken cancellationToken)
    {
        var materialUnitPrice = await _plannedMaterialCostRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(m => m.ProductUnitPrice).ThenInclude(p => p.Product)
                .Include(m => m.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes)
                .Include(m => m.NormFactor).ThenInclude(n => n.NormFactorAssignmentCodes).ThenInclude(a => a.TargetHardness)
                .Include(m => m.NormFactor).ThenInclude(n => n.Hardness)
                .Include(m => m.Output)
                .Include(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Costs)
                .Include(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.Code)
                .Include(m => m.SlideUnitPriceAssignmentCode).ThenInclude(muac => muac.Material).ThenInclude(m => m.AssignmentCode).ThenInclude(a => a.Code)
                .Include(m => m.MaterialUnitPrice).ThenInclude(m => m.MaterialUnitPriceAssignmentCodes),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var mCost = new List<PlannedMaterialCostAssignmentCode>();
        var normFactorAssignment = materialUnitPrice.NormFactor?.NormFactorAssignmentCodes
            .FirstOrDefault(x => x.AssignmentCodeId == materialUnitPrice.SlideUnitPriceAssignmentCode?.Material?.AssigmentCodeId)
            ?? materialUnitPrice.NormFactor?.NormFactorAssignmentCodes.FirstOrDefault();
        var coefficientValue = normFactorAssignment?.Value ?? 1;
        var targetHardnessId = normFactorAssignment?.TargetHardnessId;
        var targetHardnessValue = normFactorAssignment?.TargetHardness?.Value;

        if (materialUnitPrice.SlideUnitPriceAssignmentCodeId != null)
        {
            var currentSlide = materialUnitPrice.SlideUnitPriceAssignmentCode.Material;
            var originalAmount = materialUnitPrice.SlideUnitPriceAssignmentCode.Amount;
            mCost.Add(new PlannedMaterialCostAssignmentCode
            {
                AssignmentCodeId = currentSlide.AssigmentCodeId,
                AssignmentCode = currentSlide.AssignmentCode.Code.Value,
                AssignmentCodeName = currentSlide.AssignmentCode.Name,
                Costs = [ new PlannedMaterialCostDto {
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

        IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> tunnelMaterials = new List<TunnelExcavationMaterialUnitPrice>();
        if (targetHardnessId.HasValue && materialUnitPrice.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice currentTunnelMaterial)
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

        static double SumMaterialUnitPriceCost(MaterialUnitPriceEntity? unitPrice)
        {
            if (unitPrice == null)
            {
                return 0;
            }

            return unitPrice.MaterialUnitPriceAssignmentCodes?.Sum(x => x.TotalPrice) ?? 0;
        }

        TunnelExcavationMaterialUnitPrice? ResolveTargetTunnelMaterialUnitPrice(
            TunnelExcavationMaterialUnitPrice currentMaterial,
            Guid targetHardnessId,
            IReadOnlyCollection<TunnelExcavationMaterialUnitPrice> candidates)
        {
            var outputStart = materialUnitPrice.Output.StartMonth;
            var outputEnd = materialUnitPrice.Output.EndMonth;

            return candidates
                .Where(x =>
                    x.ProcessId == currentMaterial.ProcessId &&
                    x.PassportId == currentMaterial.PassportId &&
                    x.InsertItemId == currentMaterial.InsertItemId &&
                    x.SupportStepId == currentMaterial.SupportStepId &&
                    x.HardnessId == targetHardnessId &&
                    x.StartMonth <= outputStart &&
                    x.EndMonth >= outputEnd)
                .OrderByDescending(x => x.StartMonth)
                .ThenByDescending(x => x.EndMonth)
                .FirstOrDefault();
        }

        var materialCost = SumMaterialUnitPriceCost(materialUnitPrice.MaterialUnitPrice) + materialUnitPrice.MaterialUnitPrice.OtherMaterialvalue;
        if (targetHardnessId.HasValue && materialUnitPrice.MaterialUnitPrice is TunnelExcavationMaterialUnitPrice currentTunnelMaterialForCost)
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

        var normFactorValue = string.Empty;
        if (materialUnitPrice.NormFactor != null)
        {
            var hardnessValue = targetHardnessValue
                ?? materialUnitPrice.NormFactor.Hardness?.Value
                ?? string.Empty;

            normFactorValue =
                $"{coefficientValue.ToString(CultureInfo.InvariantCulture)} - {hardnessValue}";
        }

        var slideUnitPriceCost = materialUnitPrice.SlideUnitPriceAssignmentCode?.Amount ?? 0;

        var dependencies = await PlannedMaterialCostCalculationDependencyLoader.LoadAsync(
            new List<Domain.Entities.Pricing.PlannedMaterialCost> { materialUnitPrice },
            _tunnelMaterialUnitPriceRepository,
            _lowValuePerishableSupplyUnitPriceRepository,
            cancellationToken);
        var calculationResults = PlannedMaterialCostCalculator.CalculateResultsByCostId(
            new List<Domain.Entities.Pricing.PlannedMaterialCost> { materialUnitPrice },
            dependencies.TunnelMaterialUnitPrices,
            dependencies.LowValuePerishableSupplyUnitPrices);
        var calculation = calculationResults.GetValueOrDefault(materialUnitPrice.Id, new PlannedMaterialCostCalculationResult());

        var result = new PlannedMaterialCostDetailDto
        {
            Id = materialUnitPrice.Id,
            OutputId = materialUnitPrice.OutputId,
            MaterialUnitPriceId = materialUnitPrice.MaterialUnitPriceId,
            ProductUnitPriceId = materialUnitPrice.ProductUnitPriceId,
            SlideUnitPriceAssignmentCodeId = materialUnitPrice.SlideUnitPriceAssignmentCodeId,
            NormFactorId = materialUnitPrice.NormFactorId,
            StoneClampRatioReferenceId = materialUnitPrice.StoneClampRatioReferenceId,
            MaterialReferenceId = materialUnitPrice.MaterialReferenceId,
            LowValuePerishableSupplyInclusion = materialUnitPrice.LowValuePerishableSupplyInclusion,
            PlannedMaterialCostAssignmentCodes = mCost,
            MaterialCost = materialCost,
            SlideUnitPriceCost = slideUnitPriceCost,
            LowValuePerishableSupplyUnitPriceCost = calculation.LowValuePerishableSupplyUnitPriceCost,
            NormFactorValue = normFactorValue,
            TotalPlannedMaterialPrice = calculation.TotalPrice
        };
        return result;
    }
}
