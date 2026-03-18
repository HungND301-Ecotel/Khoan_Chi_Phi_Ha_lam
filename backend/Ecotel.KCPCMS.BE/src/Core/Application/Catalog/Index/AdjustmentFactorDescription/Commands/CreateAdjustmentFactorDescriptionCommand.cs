using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Commands;
public record CreateAdjustmentFactorDescriptionCommand(CreateAdjustmentFactorDescriptionDto CreateModel) : IRequest<bool>;

public class CreateAdjustmentFactorDescriptionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAdjustmentFactorDescriptionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _ajustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    public async Task<bool> Handle(CreateAdjustmentFactorDescriptionCommand request, CancellationToken cancellationToken)
    {
        bool checkAdjustmentFactorExisted = await _ajustmentFactorRepository.ExistsAsync(x => x.Id == request.CreateModel.AdjustmentFactorId);
        if (!checkAdjustmentFactorExisted)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorNotFound);
        }

        var newAdjustmentFactorDescription = Domain.Entities.Index.AdjustmentFactorDescription.Create(
            request.CreateModel.Description,
            request.CreateModel.AdjustmentFactorId,
            request.CreateModel.MaintenanceAdjustmentValue,
            request.CreateModel.ElectricityAdjustmentValue);

        await _adjustmentFactorDescriptionRepository.InsertAsync(newAdjustmentFactorDescription);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
