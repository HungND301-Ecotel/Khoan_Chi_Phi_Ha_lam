// File: Application/Catalog/Material/Commands/DeleteMaterialListCommand.cs
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Material.Commands;

public record DeleteMaterialListCommand(IList<DefaultIdType> DeleteIds) : IRequest<bool>;

public class DeleteMaterialListCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteMaterialListCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(DeleteMaterialListCommand request, CancellationToken cancellationToken)
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

        var materialsToDelete = await _materialRepository.GetAllAsync(
            predicate: x => distinctIds.Contains(x.Id),
            include: x => x.Include(x => x.UnitOfMeasure).Include(x => x.AssignmentCode).Include(t => t.Costs).Include(t => t.Code),
            disableTracking: true);

        if (materialsToDelete == null || !materialsToDelete.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        if (materialsToDelete.Count != distinctIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.MaterialNotFound);
        }

        var codes = materialsToDelete.Select(m => m.Code);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

        try
        {
            _materialRepository.Delete(materialsToDelete);
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