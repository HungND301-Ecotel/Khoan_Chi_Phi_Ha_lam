using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record UpdateStoneClampRatioCommand(UpdateStoneClampRatioDto UpdateModel) : IRequest<bool>;

public class UpdateStoneClampRatioCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateStoneClampRatioCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    public async Task<bool> Handle(UpdateStoneClampRatioCommand request, CancellationToken cancellationToken)
    {
        var existedModel = await _stoneClampRatioRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.UpdateModel.Id,
            disableTracking: true
        ) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        existedModel.Update(request.UpdateModel.Value);

        _stoneClampRatioRepository.Update(existedModel);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
