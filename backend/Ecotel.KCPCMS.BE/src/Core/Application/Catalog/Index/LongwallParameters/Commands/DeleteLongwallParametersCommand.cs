using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.LongwallParameters.Commands;
public record DeleteLongwallParametersCommand(DefaultIdType DeleteId) : IRequest<bool>;

public class DeleteLongwallParametersCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteLongwallParametersCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<Domain.Entities.Index.LongwallParameters>();
    public async Task<bool> Handle(DeleteLongwallParametersCommand request, CancellationToken cancellationToken)
    {
        var existLongwallParameters = await _longwallParametersRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.DeleteId,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        _longwallParametersRepository.Delete(existLongwallParameters);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
