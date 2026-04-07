using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record DeleteSavingsRateConfigCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteSavingsRateConfigCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteSavingsRateConfigCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(DeleteSavingsRateConfigCommand request, CancellationToken cancellationToken)
    {
        var existSavingsRateConfig = await _savingsRateConfigRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _savingsRateConfigRepository.Delete(existSavingsRateConfig);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
