using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.FixedKey;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.FixedKeys.Commands;

public record CreateFixedKeyCommand(CreateFixedKeyDto CreateModel) : IRequest<bool>;

public class CreateFixedKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateFixedKeyCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(CreateFixedKeyCommand request, CancellationToken cancellationToken)
    {
        var normalizedKey = request.CreateModel.Key.Trim().ToUpper();

        if (await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Key == normalizedKey,
                disableTracking: true) != null)
        {
            throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var fixedKey = FixedKey.Create(normalizedKey, request.CreateModel.Name, request.CreateModel.Type);
            await _fixedKeyRepository.InsertAsync(fixedKey, cancellationToken);
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