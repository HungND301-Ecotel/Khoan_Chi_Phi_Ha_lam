// File: Application/Catalog/LongwallParameters/Commands/DeleteLongwallParametersListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.LongwallParameters.Commands;

public record DeleteLongwallParametersListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteLongwallParametersListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteLongwallParametersListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<Domain.Entities.Index.LongwallParameters>();

    public async Task<bool> Handle(DeleteLongwallParametersListCommand request, CancellationToken cancellationToken)
    {
        // 1. Lo?i b? trùng l?p
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var longwallParametersToDelete = await _longwallParametersRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (longwallParametersToDelete == null || !longwallParametersToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (longwallParametersToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.PassportNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _longwallParametersRepository.Delete(longwallParametersToDelete);
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
