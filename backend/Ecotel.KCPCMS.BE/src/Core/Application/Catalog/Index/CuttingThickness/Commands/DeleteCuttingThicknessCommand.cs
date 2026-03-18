using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CuttingThickness.Commands;

public record DeleteCuttingThicknessCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteCuttingThicknessCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCuttingThicknessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<Domain.Entities.Index.CuttingThickness>();
    
    public async Task<bool> Handle(DeleteCuttingThicknessCommand request, CancellationToken cancellationToken)
    {
        var existCuttingThickness = await _cuttingThicknessRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _cuttingThicknessRepository.Delete(existCuttingThickness);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
