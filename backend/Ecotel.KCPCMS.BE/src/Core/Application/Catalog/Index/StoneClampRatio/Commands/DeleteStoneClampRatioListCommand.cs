// File: Application/Catalog/StoneClampRatio/Commands/DeleteStoneClampRatioListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record DeleteStoneClampRatioListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteStoneClampRatioListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteStoneClampRatioListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _repository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();

    public async Task<bool> Handle(DeleteStoneClampRatioListCommand request, CancellationToken cancellationToken)
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

        var ratiosToDelete = await _repository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (ratiosToDelete == null || !ratiosToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (ratiosToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.StoneClampRatioNotFound);
        }

        await unitOfWork.BeginTransactionAsync();

        try
        {
            _repository.Delete(ratiosToDelete);
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