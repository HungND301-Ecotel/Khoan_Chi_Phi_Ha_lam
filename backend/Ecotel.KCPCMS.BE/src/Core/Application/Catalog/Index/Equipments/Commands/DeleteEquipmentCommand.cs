using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;
public record DeleteEquipmentCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteEquipmentCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteEquipmentCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteEquipmentCommand request, CancellationToken cancellationToken)
    {
        var existEquipment = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            include: t => t.Include(e => e.UnitOfMeasure).Include(t => t.EquipmentParts).Include(t => t.Costs).Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _equipmentRepository.Delete(existEquipment);
            _codeRepository.Delete(existEquipment.Code);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}
