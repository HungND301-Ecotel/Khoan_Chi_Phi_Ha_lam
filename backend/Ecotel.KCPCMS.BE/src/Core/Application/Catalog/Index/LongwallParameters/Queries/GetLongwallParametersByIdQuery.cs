using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallParameters;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.LongwallParameters.Queries;
public record GetLongwallParametersByIdQuery(DefaultIdType Id) : IRequest<LongwallParametersDto>;

public class GetLongwallParametersByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLongwallParametersByIdQuery, LongwallParametersDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<Domain.Entities.Index.LongwallParameters>();
    public async Task<LongwallParametersDto> Handle(GetLongwallParametersByIdQuery request, CancellationToken cancellationToken)
    {
        var longwallParameters = await _longwallParametersRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return longwallParameters.Adapt<LongwallParametersDto>();
    }
}
