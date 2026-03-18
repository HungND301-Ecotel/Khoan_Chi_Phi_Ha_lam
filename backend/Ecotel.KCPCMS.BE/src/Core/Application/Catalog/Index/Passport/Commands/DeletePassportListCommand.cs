// File: Application/Catalog/Passport/Commands/DeletePassportListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Commands;

public record DeletePassportListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeletePassportListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePassportListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Passport> _passportRepository = unitOfWork.GetRepository<Domain.Entities.Index.Passport>();

    public async Task<bool> Handle(DeletePassportListCommand request, CancellationToken cancellationToken)
    {
        // 1. Loại bỏ trùng lặp
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var passportsToDelete = await _passportRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (passportsToDelete == null || !passportsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (passportsToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.PassportNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _passportRepository.Delete(passportsToDelete);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}