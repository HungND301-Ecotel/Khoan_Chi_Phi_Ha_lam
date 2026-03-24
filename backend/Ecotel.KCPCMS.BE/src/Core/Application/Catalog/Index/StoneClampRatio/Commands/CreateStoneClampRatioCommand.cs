using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using MediatR;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record CreateStoneClampRatioCommand(CreateStoneClampRatioDto CreateModel) : IRequest<bool>;

public class CreateStoneClampRatioCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateStoneClampRatioCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    public async Task<bool> Handle(CreateStoneClampRatioCommand request, CancellationToken cancellationToken)
    {
        var newStoneClampRatio = Domain.Entities.Index.StoneClampRatio.Create(request.CreateModel.Value);
        await _stoneClampRatioRepository.InsertAsync(newStoneClampRatio);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
