using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CargoType;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CargoType.Commands;

public record UpdateCargoTypeCommand(UpdateCargoTypeDto UpdateModel) : IRequest<bool>;

public class UpdateCargoTypeCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateCargoTypeCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _repository = unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<bool> Handle(UpdateCargoTypeCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            disableTracking: false)
            ?? throw new NotFoundException(MessageCommon.DataNotFound);

        entity.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.Note);

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