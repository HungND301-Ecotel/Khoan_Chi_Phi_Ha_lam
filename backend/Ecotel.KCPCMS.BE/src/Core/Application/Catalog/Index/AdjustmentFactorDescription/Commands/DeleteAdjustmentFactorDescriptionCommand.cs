using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Commands;
public record DeleteAdjustmentFactorDescriptionCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteAdjustmentFactorDescriptionCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAdjustmentFactorDescriptionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();
    public async Task<bool> Handle(DeleteAdjustmentFactorDescriptionCommand request, CancellationToken cancellationToken)
    {
        var existAdjustmentFactor = await _adjustmentFactorDescriptionRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _adjustmentFactorDescriptionRepository.Delete(existAdjustmentFactor);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
