using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.UnitOfMeasures.Commands;
public record DeleteUnitOfMeasureCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteUnitOfMeasureCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUnitOfMeasureCommand, bool>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(DeleteUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var existUnitOfMeasure = await _unitOfMeasureRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: query => query
                .Include(u => u.AssignmentCodes)
                .Include(u => u.Materials),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            _unitOfMeasureRepository.Delete(existUnitOfMeasure);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        return true;
    }
}
