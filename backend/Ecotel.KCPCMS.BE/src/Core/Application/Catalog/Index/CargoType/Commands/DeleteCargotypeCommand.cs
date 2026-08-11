using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CargoType.Commands;

public record DeleteCargoTypeCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteCargoTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCargoTypeCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _repository = unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<bool> Handle(DeleteCargoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.DeleteId,
            disableTracking: true)
            ?? throw new NotFoundException(MessageCommon.DataNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Delete(entity);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record DeleteCargoTypeListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteCargoTypeListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCargoTypeListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _repository = unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<bool> Handle(DeleteCargoTypeListCommand request, CancellationToken cancellationToken)
    {
        var entities = await _repository.GetAllAsync(
            predicate: x => request.DeleteIds.Contains(x.Id),
            disableTracking: true);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Delete(entities);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}