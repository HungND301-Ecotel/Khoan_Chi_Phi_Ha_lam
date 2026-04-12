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
using Shared.Constants;

namespace Application.Catalog.Pricing.ProductUnitPrice.Commands;

public record CreateProductUnitPriceCommand(CreateProductUnitPriceDto CreateModel) : IRequest<bool>;

public class CreateProductUnitPriceCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<CreateProductUnitPriceCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.ProductUnitPrice> _productUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.ProductUnitPrice>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    public async Task<bool> Handle(CreateProductUnitPriceCommand request, CancellationToken cancellationToken)
    {
        bool checkExited = await _productUnitPriceRepository.ExistsAsync(p =>
            p.ProductId == request.CreateModel.ProductId &&
            p.DepartmentId == request.CreateModel.DepartmentId &&
            p.ScenarioType == ProductUnitPriceScenarioType.Plan);
        if (checkExited)
        {
            throw new ConflictException(CustomResponseMessage.ProductUnitPriceWithProductIdAlreadyExists);
        }
        if (request.CreateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.CreateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }
        if (request.CreateModel.DepartmentId != null)
        {
            bool checkDepartmentExisted = await _departmentRepository.ExistsAsync(x => x.Id == request.CreateModel.DepartmentId);
            if (!checkDepartmentExisted)
            {
                throw new NotFoundException(CustomResponseMessage.EntityNotFound);
            }
        }
        if (!request.CreateModel.Outputs.Any())
        {
            throw new BadRequestException(CustomResponseMessage.OutputEmpty);
        }

        var newOutputs = new List<Output>();
        foreach (var item in request.CreateModel.Outputs)
        {
            if (item.OutputType == Domain.Common.Enums.OutputType.PlanOutput)
            {
                newOutputs.Add(Output.Create(0, item.StartMonth, item.EndMonth, Domain.Common.Enums.OutputType.ActualOutput));
            }
            else
            {
                newOutputs.Add(Output.Create(0, item.StartMonth, item.EndMonth, Domain.Common.Enums.OutputType.PlanOutput));
            }
        }
        newOutputs.AddRange(request.CreateModel.Outputs.Adapt<IList<Output>>());

        var newProductUnitPrice = Domain.Entities.Pricing.ProductUnitPrice.Create(
            request.CreateModel.ProductId,
            request.CreateModel.UnitOfMeasureId,
            request.CreateModel.DepartmentId,
            ProductUnitPriceScenarioType.Plan);
        newProductUnitPrice.AddOutputs(newOutputs);

        // Add ProductionOutput relationship if provided
        if (request.CreateModel.ProductionOutputId.HasValue)
        {
            newProductUnitPrice.AddProductionOutput(request.CreateModel.ProductionOutputId.Value);
        }

        await _productUnitPriceRepository.InsertAsync(newProductUnitPrice, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
