using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record DeleteStoneClampRatioCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteStoneClampRatioCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteStoneClampRatioCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.StoneClampRatio> _stoneClampRatioRepository = unitOfWork.GetRepository<Domain.Entities.Index.StoneClampRatio>();
    public async Task<bool> Handle(DeleteStoneClampRatioCommand request, CancellationToken cancellationToken)
    {
        var existPassport = await _stoneClampRatioRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _stoneClampRatioRepository.Delete(existPassport);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
