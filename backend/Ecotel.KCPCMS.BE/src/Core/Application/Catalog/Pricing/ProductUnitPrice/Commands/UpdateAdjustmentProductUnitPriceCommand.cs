using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;

public record UpdateAdjustmentProductUnitPriceCommand(UpdateAdjustmentProductUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateAdjustmentProductUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<UpdateAdjustmentProductUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<AcceptanceReportItemLog> _acceptanceReportItemLogRepository = unitOfWork.GetRepository<AcceptanceReportItemLog>();
    public async Task<bool> Handle(UpdateAdjustmentProductUnitPriceCommand request, CancellationToken cancellationToken)
    {
        bool checkExited = await _productUnitPriceRepository.ExistsAsync(p =>
            p.ProductId == request.UpdateModel.ProductId &&
            p.DepartmentId == request.UpdateModel.DepartmentId &&
            p.Id != request.UpdateModel.Id &&
            p.ScenarioType == ProductUnitPriceScenarioType.Adjustment);
        if (checkExited)
        {
            throw new ConflictException(CustomResponseMessage.ProductUnitPriceWithProductIdAlreadyExists);
        }

        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        if (request.UpdateModel.DepartmentId != null)
        {
            bool checkDepartmentExisted = await _departmentRepository.ExistsAsync(x => x.Id == request.UpdateModel.DepartmentId);
            if (!checkDepartmentExisted)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }
        }

        bool checkProductExisted = await _productRepository.ExistsAsync(x => x.Id == request.UpdateModel.ProductId);
        if (!checkProductExisted)
        {
            throw new NotFoundException(CustomResponseMessage.ProductNotFound);
        }

        var exitedProductUnitPrice = await _productUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id && p.ScenarioType == ProductUnitPriceScenarioType.Adjustment,
            include: p => p.Include(p => p.Outputs).Include(p => p.ProductUnitPriceProductionOutputs),
            disableTracking: false
        ) ?? throw new NotFoundException(MessageCommon.DataNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            exitedProductUnitPrice.Update(
                request.UpdateModel.ProductId,
                request.UpdateModel.UnitOfMeasureId,
                request.UpdateModel.DepartmentId);

            // Update ProductionOutputs relationship - smart update to avoid tracking conflicts
            var existingProductionOutputIds = exitedProductUnitPrice.ProductUnitPriceProductionOutputs
                .Select(p => p.ProductionOutputId)
                .ToList();

            var newProductionOutputIds = request.UpdateModel.ProductionOutputs?.Keys.ToList() ?? new List<Guid>();

            // Remove ProductionOutputs that are no longer in the request
            var productionOutputsToRemove = existingProductionOutputIds
                .Except(newProductionOutputIds)
                .ToList();

            foreach (var productionOutputId in productionOutputsToRemove)
            {
                exitedProductUnitPrice.RemoveProductionOutput(productionOutputId);
            }

            // Add new ProductionOutputs that don't exist yet
            foreach (var (productionOutputId, meters) in request.UpdateModel.ProductionOutputs)
            {
                // AddProductionOutput đã handle cả add lẫn update meters (theo domain entity)
                exitedProductUnitPrice.AddProductionOutput(productionOutputId, meters);
            }

            _productUnitPriceRepository.Update(exitedProductUnitPrice);

            await unitOfWork.SaveChangesAsync();

            // Update PlannedOutput in AcceptanceReportItemLog for related ProductionOutputs
            await UpdateAcceptanceReportItemLogsPlannedOutput(exitedProductUnitPrice, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);

            cacheService.InvalidateGroup(CacheSignalKey);

            return true;
        }
        catch (Exception e)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task UpdateAcceptanceReportItemLogsPlannedOutput(
        Domain.Entities.Pricing.ProductUnitPrice productUnitPrice,
        CancellationToken cancellationToken)
    {
        var planProductUnitPrice = await _productUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: p => p.ProductId == productUnitPrice.ProductId
                && p.DepartmentId == productUnitPrice.DepartmentId
                && p.ScenarioType == ProductUnitPriceScenarioType.Plan,
            include: p => p.Include(x => x.Outputs),
            disableTracking: true);

        if (planProductUnitPrice == null)
        {
            return;
        }

        // Get all ProductionOutputIds from ProductUnitPriceProductionOutputs
        var productionOutputIds = productUnitPrice.ProductUnitPriceProductionOutputs
            .Select(p => p.ProductionOutputId)
            .ToList();

        if (!productionOutputIds.Any())
        {
            return;
        }

        // Get all ProductionOutputs to get their time ranges
        var productionOutputs = await _productionOutputRepository.GetAllAsync(
            predicate: p => productionOutputIds.Contains(p.Id),
            disableTracking: true);

        if (!productionOutputs.Any())
        {
            return;
        }

        // Extract time ranges for query
        var startMonths = productionOutputs.Select(po => po.StartMonth).Distinct().ToList();
        var endMonths = productionOutputs.Select(po => po.EndMonth).Distinct().ToList();

        // Get all AcceptanceReportItemLogs that might match these time ranges
        var acceptanceReportItemLogs = await _acceptanceReportItemLogRepository.GetAllAsync(
            predicate: log => startMonths.Contains(log.PeriodStartMonth) &&
                             endMonths.Contains(log.PeriodEndMonth),
            disableTracking: false);

        if (!acceptanceReportItemLogs.Any())
        {
            return;
        }

        // Filter in memory to get exact matches
        var logsToUpdate = acceptanceReportItemLogs
            .Where(log => productionOutputs.Any(po =>
                po.StartMonth == log.PeriodStartMonth &&
                po.EndMonth == log.PeriodEndMonth))
            .ToList();

        // Update PlannedOutput for each log based on matching Output
        foreach (var log in logsToUpdate)
        {
            // Find matching ProductionOutput
            var productionOutput = productionOutputs.FirstOrDefault(po =>
                po.StartMonth == log.PeriodStartMonth &&
                po.EndMonth == log.PeriodEndMonth);

            if (productionOutput == null)
            {
                continue;
            }

            // Find matching Output based on time range
            var matchingOutput = planProductUnitPrice.Outputs.FirstOrDefault(o =>
                o.OutputType == OutputType.PlanOutput &&
                o.StartMonth <= productionOutput.StartMonth &&
                o.EndMonth >= productionOutput.EndMonth);

            if (matchingOutput != null)
            {
                log.UpdatePlannedOutput(matchingOutput.ProductionMeters, log.Note);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }
}
