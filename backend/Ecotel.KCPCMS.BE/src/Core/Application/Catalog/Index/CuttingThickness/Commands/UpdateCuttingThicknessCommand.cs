using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.CuttingThickness.Commands;

public record UpdateCuttingThicknessCommand(CuttingThicknessDto UpdateModel) : IRequest<bool>;

public class UpdateCuttingThicknessCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateCuttingThicknessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<Domain.Entities.Index.CuttingThickness>();

    public async Task<bool> Handle(UpdateCuttingThicknessCommand request, CancellationToken cancellationToken)
    {
        var existCuttingThickness = await _cuttingThicknessRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existCuttingThickness.Update(request.UpdateModel.Value);

        _cuttingThicknessRepository.Update(existCuttingThickness);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
