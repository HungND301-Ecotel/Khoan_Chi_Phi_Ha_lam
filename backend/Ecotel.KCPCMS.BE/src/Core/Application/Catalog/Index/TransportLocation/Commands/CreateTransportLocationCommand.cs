using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportLocation;
using MediatR;

namespace Application.Catalog.Index.TransportLocation.Commands;

public record CreateTransportLocationCommand(CreateTransportLocationDto CreateModel) : IRequest<bool>;

public class CreateTransportLocationCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateTransportLocationCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.TransportLocation> _repository = unitOfWork.GetRepository<Domain.Entities.Index.TransportLocation>();

    public async Task<bool> Handle(CreateTransportLocationCommand request, CancellationToken cancellationToken)
    {
        var entity = Domain.Entities.Index.TransportLocation.Create(
            request.CreateModel.Code,
            request.CreateModel.Name,
            request.CreateModel.Note,
            request.CreateModel.LocationType);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            await _repository.InsertAsync(entity, cancellationToken);
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