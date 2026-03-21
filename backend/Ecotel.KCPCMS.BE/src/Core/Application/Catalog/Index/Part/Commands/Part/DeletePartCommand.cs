using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands.Part;
public record DeletePartCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeletePartCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeletePartCommand request, CancellationToken cancellationToken)
    {
        var existMaterial = await _partRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: t => t.Include(t => t.Costs).Include(t => t.UnitOfMeasure).Include(t => t.EquipmentParts).Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {

            _partRepository.Delete(existMaterial);
            _codeRepository.Delete(existMaterial.Code);
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

