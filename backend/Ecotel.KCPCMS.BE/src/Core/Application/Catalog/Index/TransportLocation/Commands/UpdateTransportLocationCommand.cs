using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportLocation;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.TransportLocation.Commands;

public record UpdateTransportLocationCommand(UpdateTransportLocationDto UpdateModel) : IRequest<bool>;

public class UpdateTransportLocationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateTransportLocationCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository = unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<bool> Handle(UpdateTransportLocationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            disableTracking: false)
            ?? throw new NotFoundException(MessageCommon.DataNotFound);

        entity.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.Note, request.UpdateModel.LocationType);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _repository.Update(entity);
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