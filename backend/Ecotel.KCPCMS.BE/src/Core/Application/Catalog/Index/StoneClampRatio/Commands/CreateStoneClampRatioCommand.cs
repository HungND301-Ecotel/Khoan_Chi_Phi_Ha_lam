using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record CreateStoneClampRatioCommand(CreateStoneClampRatioDto CreateModel) : IRequest<bool>;

public class CreateStoneClampRatioCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateStoneClampRatioCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Domain.Entities.Index.Hardness> _hardnessRepository = unitOfWork.GetRepository<Domain.Entities.Index.Hardness>();
    public async Task<bool> Handle(CreateStoneClampRatioCommand request, CancellationToken cancellationToken)
    {
        var process = await _productProcessRepository.GetFirstOrDefaultAsync(predicate: p => p.Id == request.CreateModel.ProcessId, disableTracking: true) ??
                      throw new NotFoundException(CustomResponseMessage.ProductionProcessNotFound);

        var hardnees = await _hardnessRepository.GetFirstOrDefaultAsync(predicate: p => p.Id == request.CreateModel.HardnessId, disableTracking: true) ??
              throw new NotFoundException(CustomResponseMessage.HardnessNotFound);

        var newStoneClampRatio = Domain.Entities.Index.StoneClampRatio.Create(request.CreateModel.Value,
            request.CreateModel.CoefficientValue, request.CreateModel.HardnessId, request.CreateModel.ProcessId);
        await _stoneClampRatioRepository.InsertAsync(newStoneClampRatio);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
