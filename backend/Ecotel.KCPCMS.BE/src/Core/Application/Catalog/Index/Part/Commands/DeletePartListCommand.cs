// File: Application/Catalog/Part/Commands/DeletePartListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands;

public record DeletePartListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeletePartListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeletePartListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Domain.Entities.Index.EquipmentPart> _equipmentPartRepository = unitOfWork.GetRepository<Domain.Entities.Index.EquipmentPart>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeletePartListCommand request, CancellationToken cancellationToken)
    {
        var distinctIds = request.DeleteIds.Distinct().ToList();

        if (!distinctIds.Any())
        {
            throw new BadRequestException(CustomResponseMessage.DeletedIdsEmpty);
        }

        // Query EquipmentParts kèm Part cha
        var equipmentPartsToDelete = await _equipmentPartRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: t => t.Include(t => t.Part).ThenInclude(p => p.Code),
            disableTracking: false);

        if (equipmentPartsToDelete == null || !equipmentPartsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (equipmentPartsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.PartNotFound);
        }

        // Lấy danh sách Part cha duy nhất
        var parentParts = equipmentPartsToDelete
            .Where(ep => ep.Part != null)
            .Select(ep => ep.Part!)
            .DistinctBy(p => p.Id)
            .ToList();

        var parentPartIds = parentParts.Select(p => p.Id).ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            // Xóa các EquipmentParts được chọn
            _equipmentPartRepository.Delete(equipmentPartsToDelete);
            await unitOfWork.SaveChangesAsync();

            // Query 1 lần — lấy các partId còn EquipmentPart trong DB sau khi xóa
            var partIdsStillHaveEquipment = await _equipmentPartRepository.GetAllAsync(
                predicate: ep => parentPartIds.Contains(ep.PartId),
                disableTracking: true);

            var partIdsToKeep = partIdsStillHaveEquipment
                .Select(ep => ep.PartId)
                .ToHashSet();

            // Xóa các Part không còn EquipmentPart nào
            var partsToDelete = parentParts
                .Where(p => !partIdsToKeep.Contains(p.Id))
                .ToList();

            if (partsToDelete.Any())
            {
                _partRepository.Delete(partsToDelete);
                _codeRepository.Delete(partsToDelete.Select(p => p.Code!).ToList());
                await unitOfWork.SaveChangesAsync();
            }

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