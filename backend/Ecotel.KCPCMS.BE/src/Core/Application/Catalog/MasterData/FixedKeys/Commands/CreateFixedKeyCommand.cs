using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MasterData;
using Domain.Entities.MasterData;
using MediatR;

namespace Application.Catalog.MasterData.FixedKeys.Commands;

public record CreateFixedKeyCommand(CreateFixedKeyDto CreateModel) : IRequest<bool>;

public class CreateFixedKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateFixedKeyCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(CreateFixedKeyCommand request, CancellationToken cancellationToken)
    {
        var duplicate = await _fixedKeyRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Type == request.CreateModel.Type && x.Code == request.CreateModel.Code,
            disableTracking: true);

        if (duplicate != null)
        {
            throw new ConflictException("Fixed key code already exists in this type.");
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var fixedKey = FixedKey.Create(
                request.CreateModel.Code,
                request.CreateModel.Name,
                request.CreateModel.Type,
                request.CreateModel.IsSystem);

            await _fixedKeyRepository.InsertAsync(fixedKey, cancellationToken);
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