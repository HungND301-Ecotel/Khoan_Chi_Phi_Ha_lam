using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands;

public record CreatePartCommand(CreatePartDto CreateModel) : IRequest<bool>;

public class CreatePartCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreatePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsPartCodeExisted(request.CreateModel.Code, request.CreateModel.EquipmentId))
        {
            throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
        }

        bool checkEquipmentExisted = await _equipmentRepository.ExistsAsync(x => x.Id == request.CreateModel.EquipmentId);
        if (!checkEquipmentExisted)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        if (request.CreateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.CreateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var newPart = Domain.Entities.Index.Part.Create(request.CreateModel.Code, request.CreateModel.Name, request.CreateModel.UnitOfMeasureId, request.CreateModel.EquipmentId);

            var costList = new List<Cost>();
            foreach (var cost in request.CreateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: newPart.Id));
            }

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            newPart.AddCost(costList);

            await _partRepository.InsertAsync(newPart);

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }
    }
}
