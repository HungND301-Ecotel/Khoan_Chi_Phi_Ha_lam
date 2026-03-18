using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.UnitOfMeasure;
using Domain.Entities.Index;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.UnitOfMeasures.Commands;

public record CreateUnitOfMeasureCommand(CreateUnitOfMeasureDto CreateModel) : IRequest<bool>;

public class CreateUnitOfMeasureCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateUnitOfMeasureCommand, bool>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(CreateUnitOfMeasureCommand request, CancellationToken cancellationToken)
    {
        var checkNameExisted =
            await _unitOfMeasureRepository.AnyAsync(u => u.Name.ToLower().Equals(request.CreateModel.Name.ToLower()));
        if (checkNameExisted)
        {
            throw new ConflictException(CustomResponseMessage.UnitOfMeasureNameAlreadyExists);
        }

        var newUnitOfmeasure = UnitOfMeasure.Create(request.CreateModel.Name);
        await _unitOfMeasureRepository.InsertAsync(newUnitOfmeasure, cancellationToken);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
