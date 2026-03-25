using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;

public record UpdateProductUnitPriceCommand(UpdateProductUnitPriceDto UpdateModel) : IRequest<bool>;

public class UpdateProductUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<UpdateProductUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Product> _productRepository = unitOfWork.GetRepository<Product>();
    public async Task<bool> Handle(UpdateProductUnitPriceCommand request, CancellationToken cancellationToken)
    {
        bool checkExited = await _productUnitPriceRepository.ExistsAsync(p =>
            p.ProductId == request.UpdateModel.ProductId &&
            p.Id != request.UpdateModel.Id &&
            p.ScenarioType == ProductUnitPriceScenarioType.Plan);
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

        bool checkProductExisted = await _productRepository.ExistsAsync(x => x.Id == request.UpdateModel.ProductId);
        if (!checkProductExisted)
        {
            throw new NotFoundException(CustomResponseMessage.ProductNotFound);
        }

        if (request.UpdateModel.Outputs.Any(o => o.OutputType != request.UpdateModel.Type))
        {
            throw new ConflictException(CustomResponseMessage.PleaseProvideOnlyTheTypeOutput);
        }

        var exitedProductUnitPrice = await _productUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id,
            include: p => p.Include(p => p.Outputs),
            disableTracking: true
        ) ?? throw new NotFoundException(MessageCommon.DataNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var requestOutputs = request.UpdateModel.Outputs.ToList();
            var duplicatedOutputIds = requestOutputs
                .Where(o => o.Id != Guid.Empty)
                .GroupBy(o => o.Id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedOutputIds.Any())
            {
                throw new BadRequestException("Duplicate output id in request");
            }

            var ouputMaps = requestOutputs
                .Where(o => o.Id != Guid.Empty)
                .ToDictionary(o => o.Id, o => o);

            var insertOutputList = requestOutputs
                .Where(o => o.Id == Guid.Empty)
                .ToList();
            var deleteOutputs = new List<Output>();
            var updateOutputs = new List<Output>();
            foreach (var item in exitedProductUnitPrice.Outputs.Where(o => o.OutputType == request.UpdateModel.Type))
            {
                if (ouputMaps.TryGetValue(item.Id, out var updateOutput))
                {
                    updateOutputs.Add(Output.Create(item.Id, item.ProductionMeters, item.StartMonth, item.EndMonth, item.OutputType));
                    item.Update(updateOutput.ProductionMeters, updateOutput.StartMonth, updateOutput.EndMonth);
                }
                else
                {
                    deleteOutputs.Add(item);
                }
            }

            if (deleteOutputs.Any())
            {
                var otherDeleteOutputs = new List<Output>();
                foreach (var item in deleteOutputs)
                {
                    var otherOutputDelete = exitedProductUnitPrice.Outputs.FirstOrDefault(o => o.StartMonth == item.StartMonth && o.EndMonth == item.EndMonth && o.OutputType != item.OutputType);
                    if (otherOutputDelete != null)
                    {
                        otherDeleteOutputs.Add(otherOutputDelete);
                    }
                }
                deleteOutputs.AddRange(otherDeleteOutputs);
                _outputRepository.Delete(deleteOutputs);
            }

            if (insertOutputList.Any())
            {
                foreach (var item in insertOutputList)
                {
                    if (item.OutputType == request.UpdateModel.Type)
                    {
                        exitedProductUnitPrice.AddOutput(Output.Create(0, item.StartMonth, item.EndMonth, request.UpdateModel.Type == Domain.Common.Enums.OutputType.PlanOutput ? Domain.Common.Enums.OutputType.ActualOutput : Domain.Common.Enums.OutputType.PlanOutput));

                    }
                }
                exitedProductUnitPrice.AddOutputs(insertOutputList.Adapt<List<Output>>());
            }

            if (updateOutputs.Any())
            {
                foreach (var item in updateOutputs)
                {
                    var otherOutputUpdate = exitedProductUnitPrice.Outputs
                        .FirstOrDefault(o =>
                            o.StartMonth.Year == item.StartMonth.Year &&
                            o.StartMonth.Month == item.StartMonth.Month &&
                            o.EndMonth.Year == item.EndMonth.Year &&
                            o.EndMonth.Month == item.EndMonth.Month &&
                            o.OutputType != item.OutputType);

                    if (otherOutputUpdate != null && ouputMaps.TryGetValue(item.Id, out var updateOutput))
                    {
                        otherOutputUpdate.Update(otherOutputUpdate.ProductionMeters, updateOutput.StartMonth, updateOutput.EndMonth);
                    }
                }
            }

            exitedProductUnitPrice.Update(request.UpdateModel.ProductId, request.UpdateModel.UnitOfMeasureId);

            // Update ProductionOutput relationship
            exitedProductUnitPrice.ClearProductionOutputs();
            if (request.UpdateModel.ProductionOutputId.HasValue)
            {
                exitedProductUnitPrice.AddProductionOutput(request.UpdateModel.ProductionOutputId.Value);
            }

            _productUnitPriceRepository.Update(exitedProductUnitPrice);

            await unitOfWork.SaveChangesAsync();
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
}
