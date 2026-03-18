using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallParameters;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.LongwallParameters.Commands;
public record UpdateLongwallParametersCommand(LongwallParametersDto UpdateModel) : IRequest<bool>;

public class UpdateLongwallParametersCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateLongwallParametersCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<Domain.Entities.Index.LongwallParameters>();
    public async Task<bool> Handle(UpdateLongwallParametersCommand request, CancellationToken cancellationToken)
    {
        var existLongwallParameters = await _longwallParametersRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existLongwallParameters.Update(request.UpdateModel.Llc, request.UpdateModel.Lkc, request.UpdateModel.Mk);

        _longwallParametersRepository.Update(existLongwallParameters);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
