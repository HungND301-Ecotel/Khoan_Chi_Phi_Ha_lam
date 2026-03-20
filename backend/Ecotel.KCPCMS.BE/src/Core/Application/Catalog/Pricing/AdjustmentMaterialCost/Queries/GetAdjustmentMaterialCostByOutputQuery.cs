using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmnetMaterialCost;
using Domain.Common.Enums;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
namespace Application.Catalog.Pricing.AdjustmentMaterialCost.Queries;

public record GetAdjustmentMaterialCostByOutputQuery(DefaultIdType Id) : IRequest<AdjustmentMaterialCostDetailDto>;

public class GetAdjustmentMaterialCostByOutputQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAdjustmentMaterialCostByOutputQuery, AdjustmentMaterialCostDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    public async Task<AdjustmentMaterialCostDetailDto> Handle(GetAdjustmentMaterialCostByOutputQuery request, CancellationToken cancellationToken)
    {
        // Get planned output by Id
        var plannedOutput = await _outputRepository.GetFirstOrDefaultAsync(
            predicate: o => o.Id == request.Id,
            include: o => o
                .Include(o => o.ProductUnitPrice)
                .Include(o => o.PlannedMaterialCost).ThenInclude(pmc => pmc.StoneClampRatio)
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

        if (plannedMaterialCost.MaterialUnitPrice == null)
        {
            throw new NotFoundException(CustomResponseMessage.MaterialUnitPriceNotFound);
        }

        var mCost = new List<AdjustmentMaterialCostAssignmentCode>();

        if (plannedMaterialCost.SlideUnitPriceAssignmentCodeId != null)
        {
            var currentSlide = plannedMaterialCost.SlideUnitPriceAssignmentCode.Material;
            var originalAmount = plannedMaterialCost.SlideUnitPriceAssignmentCode.Amount;
            var coefficientValue = plannedMaterialCost.StoneClampRatio?.CoefficientValue ?? 1;
            var materialCost = currentSlide.Costs.FirstOrDefault(cc =>
                cc.StartMonth <= plannedMaterialCost.Output.StartMonth && cc.EndMonth >= plannedMaterialCost.Output.EndMonth)?.Amount ?? 0;
            mCost.Add(new AdjustmentMaterialCostAssignmentCode
            {
                AssignmentCodeId = currentSlide.AssigmentCodeId,
                AssignmentCode = currentSlide.AssignmentCode.Code.Value,
                AssignmentCodeName = currentSlide.AssignmentCode.Name,
                Costs = [ new AdjustmentMaterialCostDto {
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

        var result = new AdjustmentMaterialCostDetailDto
        {
            Id = plannedMaterialCost.Id,
            OutputId = plannedOutput.Id,
            MaterialUnitPriceId = plannedMaterialCost.MaterialUnitPriceId,
            ProductUnitPriceId = plannedMaterialCost.ProductUnitPriceId,
            SlideUnitPriceAssignmentCodeId = plannedMaterialCost.SlideUnitPriceAssignmentCodeId,
            StoneClampRatioId = plannedMaterialCost.StoneClampRatioId,
            AdjustmentMaterialCostAssignmentCodes = mCost,
            TotalPlannedMaterialPrice = plannedMaterialCost.GetTotalPrice()
        };
        return result;
    }
}