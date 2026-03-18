using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.UnitOfMeasures.Commands;
public record UpdateUnitOfMeasureCommand(UnitOfMeasureDto UpdateModel) : IRequest<bool>;

public class UpdateUnitOfMeasureCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateUnitOfMeasureCommand, bool>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(UpdateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        bool checkNameExisted =
            await _unitOfMeasureRepository.AnyAsync(u =>
                u.Name.Trim().ToLower().Equals(request.UpdateModel.Name.Trim().ToLower()) &&
                u.Id != request.UpdateModel.Id);
        if (checkNameExisted)
        {
            throw new ConflictException(CustomResponseMessage.UnitOfMeasureNameAlreadyExists);
        }

        var existUnitOfMeasure = await _unitOfMeasureRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existUnitOfMeasure.Update(request.UpdateModel.Name.Trim());

        _unitOfMeasureRepository.Update(existUnitOfMeasure);
        await unitOfWork.SaveChangesAsync();
        await unitOfWork.CommitAsync();
        return true;
    }
}
