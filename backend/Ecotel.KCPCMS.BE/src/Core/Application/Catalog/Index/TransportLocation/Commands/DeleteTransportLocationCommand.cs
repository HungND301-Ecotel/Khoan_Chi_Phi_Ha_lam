using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.TransportLocation.Commands;

public record DeleteTransportLocationCommand(Guid DeleteId) : IRequest<bool>;

public class DeleteTransportLocationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportLocationCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository = unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<bool> Handle(DeleteTransportLocationCommand request, CancellationToken cancellationToken)
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

public record DeleteTransportLocationListCommand(IList<Guid> DeleteIds) : IRequest<bool>;

public class DeleteTransportLocationListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteTransportLocationListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository = unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<bool> Handle(DeleteTransportLocationListCommand request, CancellationToken cancellationToken)
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