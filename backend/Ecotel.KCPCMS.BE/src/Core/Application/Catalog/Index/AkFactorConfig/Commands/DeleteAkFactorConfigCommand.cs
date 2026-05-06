using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record DeleteAkFactorConfigCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteAkFactorConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAkFactorConfigCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _AkFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();

    public async Task<bool> Handle(DeleteAkFactorConfigCommand request, CancellationToken cancellationToken)
    {
        var existAkFactorConfig = await _AkFactorConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _AkFactorConfigRepository.Delete(existAkFactorConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
