using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.FixedKey;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.FixedKeys.Commands;

public record UpdateFixedKeyCommand(FixedKeyDto UpdateModel) : IRequest<bool>;

public class UpdateFixedKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateFixedKeyCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(UpdateFixedKeyCommand request, CancellationToken cancellationToken)
    {
        var normalizedKey = request.UpdateModel.Key.Trim().ToUpper();
        var existingFixedKey = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Key == normalizedKey && x.Id != request.UpdateModel.Id,
                disableTracking: true) != null)
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            existingFixedKey.Update(normalizedKey, request.UpdateModel.Name, request.UpdateModel.Type);
            _fixedKeyRepository.Update(existingFixedKey);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}