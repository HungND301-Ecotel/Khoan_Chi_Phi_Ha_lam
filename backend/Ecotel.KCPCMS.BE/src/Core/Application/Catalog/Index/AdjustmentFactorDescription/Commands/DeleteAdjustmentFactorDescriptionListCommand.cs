// File: Application/Catalog/AdjustmentFactorDescription/Commands/DeleteAdjustmentFactorDescriptionListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Commands;

public record DeleteAdjustmentFactorDescriptionListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteAdjustmentFactorDescriptionListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAdjustmentFactorDescriptionListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _repository = unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();

    public async Task<bool> Handle(DeleteAdjustmentFactorDescriptionListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        // 2. Kiểm tra tồn tại
        var descriptionsToDelete = await _repository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (descriptionsToDelete == null || !descriptionsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (descriptionsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        // 3. Xóa an toàn trong transaction
        await unitOfWork.BeginTransactionAsync();

        try
        {
            _repository.Delete(descriptionsToDelete);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}