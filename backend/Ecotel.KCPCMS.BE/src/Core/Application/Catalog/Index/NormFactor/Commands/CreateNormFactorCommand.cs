using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.NormFactor;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record CreateNormFactorCommand(CreateNormFactorDto CreateModel) : IRequest<bool>;

public class CreateNormFactorCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateNormFactorCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.NormFactor> _normFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.NormFactor>();
    private readonly IWriteRepository<Domain.Entities.Index.AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<Domain.Entities.Index.AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository = unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    public async Task<bool> Handle(CreateNormFactorCommand request, CancellationToken cancellationToken)
    {
        var uniqueAssignmentIds = request.CreateModel.AssignmentCodeIds.Distinct().ToList();
        var checkProductionProcessTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProductionProcessId);
        var checkHardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId);
        var checkStoneClampRatioTask = await _stoneClampRatioRepository.AnyAsync(p => p.Id == request.CreateModel.StoneClampRatioId);
        var checkReferenceNormFactor = request.CreateModel.ReferenceNormAdjustmentFactorId == null ? true : await _normFactorRepository.AnyAsync(p => p.Id == request.CreateModel.ReferenceNormAdjustmentFactorId);
        var countExistingTask = await _assignmentCodeRepository.CountAsync(predicate: a => uniqueAssignmentIds.Contains(a.Id));

        if (!checkProductionProcessTask)
        {
            throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);
        }

        if (!checkHardnessTask)
        {
            throw new NotFoundException(CustomResponseMessage.HardnessNotFound);
        }

        if (!checkStoneClampRatioTask)
        {
            throw new NotFoundException(CustomResponseMessage.StoneClampRatioNotFound);
        }

        if (!checkReferenceNormFactor)
        {
            throw new NotFoundException(CustomResponseMessage.NormFactorNotFound);
        }

        if (countExistingTask != uniqueAssignmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var normFactor = Domain.Entities.Index.NormFactor.Create(request.CreateModel.ProductionProcessId, request.CreateModel.HardnessId, request.CreateModel.StoneClampRatioId, request.CreateModel.Value, request.CreateModel.ReferenceNormAdjustmentFactorId);
        normFactor.AddNormFactorAssignmentCode(uniqueAssignmentIds.Select(a => NormFactorAssignmentCode.Create(a, Guid.Empty)).ToList());
        await _normFactorRepository.InsertAsync(normFactor, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}

