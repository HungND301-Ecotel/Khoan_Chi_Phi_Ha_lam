using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.NormFactor;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record UpdateNormFactorCommand(UpdateNormFactorDto UpdateModel) : IRequest<bool>;

public class UpdateNormFactorCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateNormFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.NormFactor> _normFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.NormFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository = unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.NormFactorAssignmentCode> _normFactorAssignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.NormFactorAssignmentCode>();

    public async Task<bool> Handle(UpdateNormFactorCommand request, CancellationToken cancellationToken)
    {
        var existNormFactor = await _normFactorRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: p => p.Include(p => p.NormFactorAssignmentCodes),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var uniqueAssignmentIds = request.UpdateModel.AssignmentCodeIds.Distinct().ToList();

        var checkProductionProcessTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.UpdateModel.ProductionProcessId);
        var checkHardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.UpdateModel.HardnessId)
              && (!request.UpdateModel.TargetHardnessId.HasValue
                  || await _hardnessRepository.AnyAsync(p => p.Id == request.UpdateModel.TargetHardnessId.Value));
        var checkStoneClampRatioTask = await _stoneClampRatioRepository.AnyAsync(p => p.Id == request.UpdateModel.StoneClampRatioId);
        var countExistingTask = await _assignmentCodeRepository.CountAsync(predicate: a => uniqueAssignmentIds.Contains(a.Id));

        if (!checkProductionProcessTask)
        {
            throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);
        }

        if (request.UpdateModel.HardnessId != null)
        {
            if (!checkHardnessTask)
            {
                throw new NotFoundException(CustomResponseMessage.HardnessNotFound);
            }
        }

        if (!checkStoneClampRatioTask)
        {
            throw new NotFoundException(CustomResponseMessage.StoneClampRatioNotFound);
        }

        if (countExistingTask != uniqueAssignmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        // update main properties
        existNormFactor.Update(request.UpdateModel.ProductionProcessId, request.UpdateModel.HardnessId, request.UpdateModel.StoneClampRatioId, request.UpdateModel.Value, request.UpdateModel.TargetHardnessId, request.UpdateModel.SteelMeshType);

        // determine assignment codes to add / keep / remove
        var existingAssignmentCodes = existNormFactor.NormFactorAssignmentCodes.ToList();
        var existingAssignmentIds = existingAssignmentCodes.Select(e => e.AssignmentCodeId).ToList();

        var toAdd = uniqueAssignmentIds.Except(existingAssignmentIds).ToList();
        var toKeep = existingAssignmentCodes.Where(e => uniqueAssignmentIds.Contains(e.AssignmentCodeId)).ToList();
        var toRemove = existingAssignmentCodes.Where(e => !uniqueAssignmentIds.Contains(e.AssignmentCodeId)).ToList();

        var newList = new List<Domain.Entities.Index.NormFactorAssignmentCode>();
        // keep existing
        newList.AddRange(toKeep);
        // create new ones
        newList.AddRange(toAdd.Select(a => Domain.Entities.Index.NormFactorAssignmentCode.Create(a, existNormFactor.Id)));

        // replace assignment codes in aggregate
        existNormFactor.AddNormFactorAssignmentCode(newList);

        // delete removed assignment codes from repository so they are removed from DB
        if (toRemove.Any())
        {
            _normFactorAssignmentCodeRepository.Delete(toRemove);
        }

        _normFactorRepository.Update(existNormFactor);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
