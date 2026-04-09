using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands.Part;

public record CreatePartCommand(CreatePartDto CreateModel) : IRequest<bool>;

public class CreatePartCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<CreatePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(CreatePartCommand request, CancellationToken cancellationToken)
    {
        if (await codeService.IsPartCodeExisted(request.CreateModel.Code))
        {
            throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
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
            var newPart = Domain.Entities.Index.Part.Create(
                request.CreateModel.Code,
                request.CreateModel.Name,
                request.CreateModel.UnitOfMeasureId,
                request.CreateModel.ReplacementTimeStandard,
                request.CreateModel.PartType);

            var costList = new List<Cost>();
            foreach (var cost in request.CreateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: CostType.Part,
                    amount: cost.Amount,
                    costTypeId: newPart.Id,
                    actualAmount: cost.ActualAmount));
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
