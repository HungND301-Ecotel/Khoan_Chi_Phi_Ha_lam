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
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();

    public async Task<bool> Handle(CreateNormFactorCommand request, CancellationToken cancellationToken)
    {
        var assignmentConfigs = request.CreateModel.AssignmentCodes ?? [];
        if (!assignmentConfigs.Any())
        {
            throw new BadRequestException("Thành phần điều chỉnh định mức không được để trống.");
        }

        var uniqueConfigs = assignmentConfigs
            .Select(a => new { a.AssignmentCodeId, a.MaterialId })
            .Distinct()
            .ToList();
        if (uniqueConfigs.Count != assignmentConfigs.Count)
        {
            throw new BadRequestException("Vật tư bị trùng lặp trong thành phần điều chỉnh định mức.");
        }

        var checkProductionProcess = await _productionProcessRepository.AnyAsync(p => p.Id == request.CreateModel.ProductionProcessId);
        if (!checkProductionProcess)
        {
            throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);
        }

        if (request.CreateModel.StoneClampRatioId.HasValue)
        {
            var checkStoneClampRatio = await _stoneClampRatioRepository.AnyAsync(p => p.Id == request.CreateModel.StoneClampRatioId.Value);
            if (!checkStoneClampRatio)
            {
                throw new NotFoundException(CustomResponseMessage.StoneClampRatioNotFound);
            }
        }

        if (request.CreateModel.HardnessId.HasValue)
        {
            var checkHardness = await _hardnessRepository.AnyAsync(p => p.Id == request.CreateModel.HardnessId.Value);
            if (!checkHardness)
            {
                throw new NotFoundException(CustomResponseMessage.HardnessNotFound);
            }
        }

        var uniqueAssignmentIds = assignmentConfigs.Select(a => a.AssignmentCodeId).Distinct().ToList();
        var assignmentCount = await _assignmentCodeRepository.CountAsync(predicate: a => uniqueAssignmentIds.Contains(a.Id));
        if (assignmentCount != uniqueAssignmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var uniqueMaterialIds = assignmentConfigs.Select(a => a.MaterialId).Distinct().ToList();
        var materialCount = await _materialRepository.CountAsync(predicate: m => uniqueMaterialIds.Contains(m.Id));
        if (materialCount != uniqueMaterialIds.Count)
        {
            throw new NotFoundException("Có vật tư không tồn tại trong hệ thống.");
        }

        var targetHardnessIds = assignmentConfigs
            .Where(x => x.TargetHardnessId.HasValue)
            .Select(x => x.TargetHardnessId!.Value)
            .Distinct()
            .ToList();
        if (targetHardnessIds.Count > 0)
        {
            var targetHardnessCount = await _hardnessRepository.CountAsync(predicate: h => targetHardnessIds.Contains(h.Id));
            if (targetHardnessCount != targetHardnessIds.Count)
            {
                throw new NotFoundException(CustomResponseMessage.HardnessNotFound);
            }
        }

        var normFactor = NormFactor.Create(
            request.CreateModel.ProductionProcessId,
            request.CreateModel.HardnessId,
            request.CreateModel.StoneClampRatioId,
            request.CreateModel.SteelMeshType);

        normFactor.AddNormFactorAssignmentCode(
            assignmentConfigs.Select(a =>
                NormFactorAssignmentCode.Create(
                    assignmentCodeId: a.AssignmentCodeId,
                    materialId: a.MaterialId,
                    value: a.Value,
                    targetHardnessId: a.TargetHardnessId))
            .ToList());

        await _normFactorRepository.InsertAsync(normFactor, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}