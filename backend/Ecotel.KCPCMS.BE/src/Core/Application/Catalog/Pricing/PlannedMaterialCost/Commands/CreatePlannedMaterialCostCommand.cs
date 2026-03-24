using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.PlannedMaterialCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.PlannedMaterialCost.Commands;

public record CreatePlannedMaterialCostCommand(CreatePlannedMaterialCostDto CreateModel) : IRequest<bool>;

public class CreatePlannedMaterialCostCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreatePlannedMaterialCostCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<NormFactor> _normFactorRepository = unitOfWork.GetRepository<NormFactor>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<SlideUnitPriceAssignmentCode> _slideUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<SlideUnitPriceAssignmentCode>();
    public async Task<bool> Handle(CreatePlannedMaterialCostCommand request, CancellationToken cancellationToken)
    {
        bool checkExited = await _plannedMaterialCostRepository.ExistsAsync(p => p.MaterialUnitPriceId == request.CreateModel.MaterialUnitPriceId && p.ProductUnitPriceId == request.CreateModel.ProductUnitPriceId && p.OutputId == request.CreateModel.OutputId);
        if (checkExited)
        {
            throw new ConflictException(CustomResponseMessage.PlannedMaterialUnitPriceAlreadyExists);
        }
        if (request.CreateModel.NormFactorId != null)
        {
            bool checkNormFactor =
                await _normFactorRepository.ExistsAsync(p => p.Id == request.CreateModel.NormFactorId);
            if (!checkNormFactor)
            {
                throw new NotFoundException(CustomResponseMessage.NormFactorNotFound);
            }
        }

        bool checkOutput = await _outputRepository.ExistsAsync(p => p.Id == request.CreateModel.OutputId && p.OutputType == Domain.Common.Enums.OutputType.PlanOutput);
        if (!checkOutput)
        {
            throw new NotFoundException(CustomResponseMessage.OutputNotFound);
        }

        if (request.CreateModel.SlideUnitPriceAssignmentCodeId != null)
        {
            bool checkSlideUnitPriceAssignmentCode =
                await _slideUnitPriceAssignmentCodeRepository.ExistsAsync(p => p.Id == request.CreateModel.SlideUnitPriceAssignmentCodeId);
            if (!checkSlideUnitPriceAssignmentCode)
            {
                throw new NotFoundException(CustomResponseMessage.SlideUnitPriceAssignmentCodeNotFound);
            }
        }

        var newPlannedMaterialCost = Domain.Entities.Pricing.PlannedMaterialCost.Create(request.CreateModel.ProductUnitPriceId, request.CreateModel.MaterialUnitPriceId, request.CreateModel.SlideUnitPriceAssignmentCodeId, request.CreateModel.NormFactorId, request.CreateModel.OutputId);

        await _plannedMaterialCostRepository.InsertAsync(newPlannedMaterialCost, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
