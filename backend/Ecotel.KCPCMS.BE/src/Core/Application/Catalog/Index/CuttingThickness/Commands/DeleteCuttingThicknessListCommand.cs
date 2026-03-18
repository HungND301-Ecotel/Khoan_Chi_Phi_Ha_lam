using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CuttingThickness.Commands;

public record DeleteCuttingThicknessListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteCuttingThicknessListCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteCuttingThicknessListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<Domain.Entities.Index.CuttingThickness>();

    public async Task<bool> Handle(DeleteCuttingThicknessListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (distinctIds.Count != request.DeleteIds.Count)
        {
            throw new ConflictException(CustomResponseMessage.DeletedIdDuplicated);
        }

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        var cuttingThicknessToDelete = await _cuttingThicknessRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            disableTracking: true);

        if (cuttingThicknessToDelete == null || !cuttingThicknessToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (cuttingThicknessToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.PassportNotFound);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _cuttingThicknessRepository.Delete(cuttingThicknessToDelete);
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
