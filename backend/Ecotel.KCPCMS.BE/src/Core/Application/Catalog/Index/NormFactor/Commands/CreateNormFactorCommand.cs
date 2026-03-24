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
    private readonly IWriteRepository<NormFactor> _normFactorRepository = unitOfWork.GetRepository<NormFactor>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    public async Task<bool> Handle(CreateNormFactorCommand request, CancellationToken cancellationToken)
    {
        var uniqueAssignmentIds = request.CreateModel.AssignmentCodeIds.Distinct().ToList();
        var checkProductionProcessTask = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProductionProcessId);
        var checkHardnessTask = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId)
              && (!request.CreateModel.TargetHardnessId.HasValue
                  || await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.TargetHardnessId.Value));
        var checkStoneClampRatioTask = await _stoneClampRatioRepository.AnyAsync(p => p.Id == request.CreateModel.StoneClampRatioId);
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

        if (countExistingTask != uniqueAssignmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var normFactor = NormFactor.Create(request.CreateModel.ProductionProcessId, request.CreateModel.HardnessId, request.CreateModel.StoneClampRatioId, request.CreateModel.Value, request.CreateModel.TargetHardnessId);
        normFactor.AddNormFactorAssignmentCode(uniqueAssignmentIds.Select(a => NormFactorAssignmentCode.Create(a, Guid.Empty)).ToList());
        await _normFactorRepository.InsertAsync(normFactor, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}

