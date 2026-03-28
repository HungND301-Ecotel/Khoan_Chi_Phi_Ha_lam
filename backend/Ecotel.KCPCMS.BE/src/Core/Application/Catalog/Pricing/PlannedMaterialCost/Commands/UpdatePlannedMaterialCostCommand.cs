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

public record UpdatePlannedMaterialCostCommand(UpdatePlannedMaterialCostDto UpdateModel) : IRequest<bool>;

public class UpdatePlannedMaterialCostCommandHandler(
    ICacheService cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdatePlannedMaterialCostCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.PlannedMaterialCost> _plannedMaterialCostRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.PlannedMaterialCost>();
    private readonly IWriteRepository<NormFactor> _normFactorRepository = unitOfWork.GetRepository<NormFactor>();
    private readonly IWriteRepository<Output> _outputRepository = unitOfWork.GetRepository<Output>();
    private readonly IWriteRepository<SlideUnitPriceAssignmentCode> _slideUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<SlideUnitPriceAssignmentCode>();
    private readonly IWriteRepository<StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<StoneClampRatio>();
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();
    public async Task<bool> Handle(UpdatePlannedMaterialCostCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdateModel.NormFactorId != null)
        {
            bool checkNormFactor =
                await _normFactorRepository.ExistsAsync(p => p.Id == request.UpdateModel.NormFactorId);
            if (!checkNormFactor)
            {
                throw new NotFoundException(CustomResponseMessage.NormFactorNotFound);
            }
        }

        if (request.UpdateModel.StoneClampRatioReferenceId != null)
        {
            bool checkStoneClampRatio =
                await _stoneClampRatioRepository.ExistsAsync(p => p.Id == request.UpdateModel.StoneClampRatioReferenceId);
            if (!checkStoneClampRatio)
            {
                throw new NotFoundException(CustomResponseMessage.StoneClampRatioNotFound);
            }
        }

        if (request.UpdateModel.MaterialReferenceId != null)
        {
            bool checkStoneClampRatio =
                await _materialRepository.ExistsAsync(p => p.Id == request.UpdateModel.MaterialReferenceId);
            if (!checkStoneClampRatio)
            {
                throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
            }
        }

        bool checkOutput = await _outputRepository.ExistsAsync(p => p.Id == request.UpdateModel.OutputId && p.OutputType == Domain.Common.Enums.OutputType.PlanOutput);
        if (!checkOutput)
        {
            throw new NotFoundException(CustomResponseMessage.OutputNotFound);
        }

        if (request.UpdateModel.SlideUnitPriceAssignmentCodeId != null)
        {
            bool checkSlideUnitPriceAssignmentCode =
                await _slideUnitPriceAssignmentCodeRepository.ExistsAsync(p => p.Id == request.UpdateModel.SlideUnitPriceAssignmentCodeId);
            if (!checkSlideUnitPriceAssignmentCode)
            {
                throw new NotFoundException(CustomResponseMessage.SlideUnitPriceAssignmentCodeNotFound);
            }
        }

        var existPlannedMaterial = await _plannedMaterialCostRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id,
            disableTracking: true
        ) ?? throw new NotFoundException(CustomResponseMessage.PlannedMaterialCostNotFound);

        bool checkExited = await _plannedMaterialCostRepository.ExistsAsync(p => p.MaterialUnitPriceId == request.UpdateModel.MaterialUnitPriceId && p.ProductUnitPriceId == existPlannedMaterial.ProductUnitPriceId && p.OutputId == request.UpdateModel.OutputId && p.Id != request.UpdateModel.Id);
        if (checkExited)
        {
            throw new ConflictException(CustomResponseMessage.PlannedMaterialUnitPriceAlreadyExists);
        }

        existPlannedMaterial.Update(request.UpdateModel.MaterialUnitPriceId,
            request.UpdateModel.SlideUnitPriceAssignmentCodeId, request.UpdateModel.NormFactorId,
            request.UpdateModel.StoneClampRatioReferenceId,
            request.UpdateModel.MaterialReferenceId,
            request.UpdateModel.OutputId);

        _plannedMaterialCostRepository.Update(existPlannedMaterial);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
