using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using MediatR;

namespace Application.Catalog.Index.CuttingThickness.Commands;

public record CreateCuttingThicknessCommand(CreateCuttingThicknessDto CreateModel) : IRequest<bool>;

public class CreateCuttingThicknessCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateCuttingThicknessCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<Domain.Entities.Index.CuttingThickness>();

    public async Task<bool> Handle(CreateCuttingThicknessCommand request, CancellationToken cancellationToken)
    {
        var newCuttingThickness = Domain.Entities.Index.CuttingThickness.Create(request.CreateModel.Value);
        await _cuttingThicknessRepository.InsertAsync(newCuttingThickness);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
