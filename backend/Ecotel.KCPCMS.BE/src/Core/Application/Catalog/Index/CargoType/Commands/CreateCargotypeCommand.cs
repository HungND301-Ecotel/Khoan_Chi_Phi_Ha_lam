using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CargoType;
using Domain.Entities.Index;
using MediatR;

namespace Application.Catalog.Index.CargoType.Commands;

public record CreateCargoTypeCommand(CreateCargoTypeDto CreateModel) : IRequest<bool>;

public class CreateCargoTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateCargoTypeCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _repository = unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<bool> Handle(CreateCargoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = Domain.Entities.Index.CargoType.Create(
            request.CreateModel.Code,
            request.CreateModel.Name,
            request.CreateModel.Note);

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