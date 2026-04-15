using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;

public record DeleteEquipmentListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteEquipmentListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteEquipmentListCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(DeleteEquipmentListCommand request, CancellationToken cancellationToken)
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

        var equipmentsToDelete = await _equipmentRepository.GetAllAsync(
            predicate: e => distinctIds.Contains(e.Id),
            include: e => e.Include(t => t.EquipmentParts).Include(t => t.Costs).Include(t => t.Code),
            disableTracking: true);

        if (equipmentsToDelete == null || !equipmentsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (equipmentsToDelete.Count != distinctIds.Count)
        {
            throw new BadRequestException(CustomResponseMessage.EquipmentNotFound);
        }

        var codes = equipmentsToDelete.Select(e => e.Code);
        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _equipmentRepository.Delete(equipmentsToDelete);
            _codeRepository.Delete(codes);
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
