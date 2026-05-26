using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Material.Commands
{
    public record CreateMaterialCommand(CreateMaterialDto CreateModel) : IRequest<bool>;

    public class CreateMaterialCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreateMaterialCommand, bool>
    {
        private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
        private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
        public async Task<bool> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
        {
            if (await codeService.IsCodeExisted(request.CreateModel.Code))
            {
                throw new ConflictException(CustomResponseMessage.MaterialCodeAlreadyExists);
            }

            if (request.CreateModel.UnitOfMeasureId != null)
            {
                bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.CreateModel.UnitOfMeasureId);
                if (!checkUnitOfMeasureExisted)
                {
                    throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
                }
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            try
            {
                var normalizedMaterialType = request.CreateModel.MaterialType == Domain.Common.Enums.MaterialType.MaterialOutContract
                    ? Domain.Common.Enums.MaterialType.MaterialInContract
                    : request.CreateModel.MaterialType;
                var newMaterial = Domain.Entities.Index.Material.Create(request.CreateModel.Code, request.CreateModel.Name, request.CreateModel.UnitOfMeasureId, request.CreateModel.AssigmentCodeId, normalizedMaterialType);

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
