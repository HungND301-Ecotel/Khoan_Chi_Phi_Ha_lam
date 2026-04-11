using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedMaintainCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedMaintainCost.Commands;
public record UpdatePlannedMaintainCostCommand(UpdatePlannedMaintainCostDto UpdateModel) : IRequest<bool>;

public class UpdatePlannedMaintainCostCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePlannedMaintainCostCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaintainCost> _plannedMaintainCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaintainCost>();
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaintainCostAdjustmentFactor> _plannedMaintainCostAdjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaintainCostAdjustmentFactor>();
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Pricing.MaintainUnitPrice> _maintainUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaintainUnitPrice>();
    public async Task<bool> Handle(UpdatePlannedMaintainCostCommand request, CancellationToken cancellationToken)
    {
        var checkProductUnitPrice = await _productUnitPriceRepository.ExistsAsync(p => p.Id == request.UpdateModel.ProductUnitPriceId);
        if (!checkProductUnitPrice)
        {
            throw new NotFoundException(CustomResponseMessage.ProductUnitPriceNotFound);
        }

        var plannedMaintainCost = await _plannedMaintainCostRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id,
            include: p => p.Include(p => p.PlannedMaintainCostAdjustmentFactors),
            disableTracking: true
            ) ?? throw new NotFoundException(CustomResponseMessage.PlannedMaintainCostNotFound);

        bool checkOutput = await _outputRepository.ExistsAsync(p => p.Id == request.UpdateModel.OutputId && p.OutputType == Domain.Common.Enums.OutputType.PlanOutput);
        if (!checkOutput)
        {
            throw new NotFoundException(CustomResponseMessage.OutputNotFound);
        }

        var maintainUnitPrices = request.UpdateModel.Costs.Select(c => c.MaintainUnitPriceId).ToList();
        int countMaintainUnitPrices = await _maintainUnitPriceRepository.CountAsync(p => maintainUnitPrices.Contains(p.Id));

        if (countMaintainUnitPrices != maintainUnitPrices.Count)
        {
            throw new NotFoundException(CustomResponseMessage.MaintainUnitPriceNotFound);
        }

        var adjustmentFactorIds = request.UpdateModel.Costs
            .SelectMany(c => c.AdjustmentFactorDescriptions)
            .Distinct()
            .ToList();

        if (!adjustmentFactorIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.AdjustmentFactorDescriptionEmpty);
        }

        var adjDescriptions = await _adjustmentFactorDescriptionRepository.GetAllAsync(
            predicate: p => adjustmentFactorIds.Contains(p.Id),
            disableTracking: true);

        var adjMap = adjDescriptions.ToDictionary(a => a.Id, a => a);

        if (adjDescriptions.Count != adjustmentFactorIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorDescriptionNotFound);
        }

        var costs = request.UpdateModel.Costs.Select(c => PlannedMaintainCostAdjustmentFactor.Create(Guid.Empty, c.MaintainUnitPriceId, c.Quantity, c.K6AdjustmentFactorValue, c.AdjustmentFactorDescriptions.Select(a => adjMap.GetValueOrDefault(a)).ToList())).ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _plannedMaintainCostAdjustmentFactorRepository.Delete(plannedMaintainCost.PlannedMaintainCostAdjustmentFactors);

            plannedMaintainCost.ClearPlannedMaintainCostAdjustmentFactors();
            plannedMaintainCost.Update(
                request.UpdateModel.ProductUnitPriceId,
                request.UpdateModel.OutputId,
                request.UpdateModel.TrimmingCoefficient);
            plannedMaintainCost.AddPlannedMaintainCostAdjustmentFactors(costs);

            _plannedMaintainCostRepository.Update(plannedMaintainCost);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken: cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
