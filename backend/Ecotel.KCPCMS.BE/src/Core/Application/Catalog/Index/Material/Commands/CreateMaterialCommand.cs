using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Material.Commands
{
    public record CreateMaterialCommand(CreateMaterialDto CreateModel) : IRequest<bool>;

    public class CreateMaterialCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateMaterialCommand, bool>
    {
        private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
        private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
        private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
        // TODO: REMOVE after data migration is complete - used only for orphan Code recovery
        private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();

        public async Task<bool> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
        {
            if (request.CreateModel.UnitOfMeasureId != null)
            {
                bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.CreateModel.UnitOfMeasureId);
                if (!checkUnitOfMeasureExisted)
                {
                    throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
                }
            }

            var normalizedMaterialType = request.CreateModel.MaterialType == Domain.Common.Enums.MaterialType.MaterialOutContract
                ? Domain.Common.Enums.MaterialType.MaterialInContract
                : request.CreateModel.MaterialType;

            var deletedMaterial = await _materialRepository.GetFirstOrDefaultAsync(
                predicate: m => m.Code.Value == request.CreateModel.Code.ToUpper() && m.DeletedOn != null,
                include: q => q.Include(m => m.Code).Include(m => m.Costs),
                disableTracking: false,
                ignoreQueryFilters: true);

            await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            try
            {
                if (deletedMaterial != null)
                {
                    deletedMaterial.Restore();
                    deletedMaterial.Code?.Restore();
                    deletedMaterial.Update(
                        request.CreateModel.Code,
                        request.CreateModel.Name,
                        request.CreateModel.UnitOfMeasureId,
                        request.CreateModel.AssigmentCodeId,
                        normalizedMaterialType);

                    var oldCosts = deletedMaterial.Costs.ToList();
                    if (oldCosts.Any())
                    {
                        _costRepository.Delete(oldCosts);
                    }

                    var costList = new List<Cost>();
                    foreach (var cost in request.CreateModel.Costs)
                    {
                        costList.Add(Cost.Create(
                            startMonth: cost.StartMonth,
                            endMonth: cost.EndMonth,
                            costType: cost.CostType,
                            amount: cost.Amount,
                            actualAmount: cost.ActualAmount,
                            costTypeId: deletedMaterial.Id));
                    }

                    if (costList.HasOverlap())
                    {
                        throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
                    }

                    deletedMaterial.AddMaterialCost(costList);
                }
                else
                {
                    // TODO: REMOVE block below after data migration is complete - orphan Code recovery
                    var orphanCode = await _codeRepository.GetFirstOrDefaultAsync(
                        predicate: c => c.Value == request.CreateModel.Code.ToUpper() && c.Material == null,
                        disableTracking: false,
                        ignoreQueryFilters: true);

                    if (orphanCode == null && await codeService.IsCodeExisted(request.CreateModel.Code))
                    {
                        throw new ConflictException(CustomResponseMessage.MaterialCodeAlreadyExists);
                    }

                    Domain.Entities.Index.Material newMaterial;

                    if (orphanCode != null)
                    {
#pragma warning disable CS0618
                        newMaterial = Domain.Entities.Index.Material.CreateWithExistingCode(
                            orphanCode.Id,
                            request.CreateModel.Name,
                            request.CreateModel.UnitOfMeasureId,
                            request.CreateModel.AssigmentCodeId,
                            normalizedMaterialType);
#pragma warning restore CS0618
                    }
                    // TODO: END REMOVE
                    else
                    {
                        newMaterial = Domain.Entities.Index.Material.Create(
                            request.CreateModel.Code,
                            request.CreateModel.Name,
                            request.CreateModel.UnitOfMeasureId,
                            request.CreateModel.AssigmentCodeId,
                            normalizedMaterialType);
                    }

                    var costList = new List<Cost>();
                    foreach (var cost in request.CreateModel.Costs)
                    {
                        costList.Add(Cost.Create(
                            startMonth: cost.StartMonth,
                            endMonth: cost.EndMonth,
                            costType: cost.CostType,
                            amount: cost.Amount,
                            actualAmount: cost.ActualAmount,
                            costTypeId: newMaterial.Id));
                    }

                    if (costList.HasOverlap())
                    {
                        throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
                    }

                    newMaterial.AddMaterialCost(costList);
                    await _materialRepository.InsertAsync(newMaterial, cancellationToken);
                }

                await unitOfWork.SaveChangesAsync();
                await unitOfWork.CommitAsync(cancellationToken);
            }
            catch
            {
                await unitOfWork.RollbackAsync(cancellationToken);
                throw;
            }
            return true;
        }
    }
}