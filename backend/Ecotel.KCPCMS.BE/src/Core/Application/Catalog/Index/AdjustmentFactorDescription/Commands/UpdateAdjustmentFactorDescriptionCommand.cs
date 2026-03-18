using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Commands;
public record UpdateAdjustmentFactorDescriptionCommand(UpdateAdjustmentFactorDescriptionDto UpdateModel) : IRequest<bool>;

public class UpdateAdjustmentFactorDescriptionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAdjustmentFactorDescriptionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();
    public async Task<bool> Handle(UpdateAdjustmentFactorDescriptionCommand request, CancellationToken cancellationToken)
    {
        bool checkAdjustmentFactorExisted = await _adjustmentFactorRepository.ExistsAsync(x => x.Id == request.UpdateModel.AdjustmentFactorId);
        if (!checkAdjustmentFactorExisted)
        {
            throw new NotFoundException(CustomResponseMessage.AdjustmentFactorNotFound);
        }

        var existAdjustmentFactorDescription = await _adjustmentFactorDescriptionRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existAdjustmentFactorDescription.Update(
            request.UpdateModel.Description,
            request.UpdateModel.AdjustmentFactorId,
            request.UpdateModel.MaintenanceAdjustmentValue,
            request.UpdateModel.ElectricityAdjustmentValue);

        _adjustmentFactorDescriptionRepository.Update(existAdjustmentFactorDescription);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
