using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MasterData;
using Domain.Entities.MasterData;
using MediatR;

namespace Application.Catalog.MasterData.FixedKeys.Commands;

public record UpdateFixedKeyCommand(FixedKeyDto UpdateModel) : IRequest<bool>;

public class UpdateFixedKeyCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateFixedKeyCommand, bool>
{
    private readonly IWriteRepository<FixedKey> _fixedKeyRepository = unitOfWork.GetRepository<FixedKey>();

    public async Task<bool> Handle(UpdateFixedKeyCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            var existing = await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Id == request.UpdateModel.Id,
                disableTracking: false) ?? throw new NotFoundException("Fixed key not found.");

            var duplicate = await _fixedKeyRepository.GetFirstOrDefaultAsync(
                predicate: x => x.Id != request.UpdateModel.Id &&
                                x.Type == request.UpdateModel.Type &&
                                x.Code == request.UpdateModel.Code,
                disableTracking: true);

            if (duplicate != null)
            {
                throw new ConflictException("Fixed key code already exists in this type.");
            }

            existing.Update(request.UpdateModel.Code, request.UpdateModel.Name);
            _fixedKeyRepository.Update(existing);

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