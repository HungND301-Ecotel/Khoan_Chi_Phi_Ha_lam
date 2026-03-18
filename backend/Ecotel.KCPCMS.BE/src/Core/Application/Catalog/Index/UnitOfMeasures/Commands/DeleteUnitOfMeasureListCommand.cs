// File: Application/Catalog/UnitOfMeasures/Commands/DeleteUnitOfMeasureListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.UnitOfMeasures.Commands;

public record DeleteUnitOfMeasureListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteUnitOfMeasureListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUnitOfMeasureListCommand, bool>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();

    public async Task<bool> Handle(DeleteUnitOfMeasureListCommand request, CancellationToken cancellationToken)
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

        var unitOfMeasuresToDelete = await _unitOfMeasureRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: query => query
                .Include(u => u.Equipments)
                .Include(u => u.AssignmentCodes)
                .Include(u => u.Materials)
                .Include(u => u.Parts),
            disableTracking: true);

        if (unitOfMeasuresToDelete == null || !unitOfMeasuresToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (unitOfMeasuresToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        // 4. Xóa an toàn trong transaction
        await unitOfWork.BeginTransactionAsync();

        try
        {
            _unitOfMeasureRepository.Delete(unitOfMeasuresToDelete);
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