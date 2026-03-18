using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Passport;
using Mapster;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Queries;
public record GetPassportByIdQuery(DefaultIdType Id) : IRequest<PassportDto>;

public class GetPassportByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetPassportByIdQuery, PassportDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Passport> _passportRepository = unitOfWork.GetRepository<Domain.Entities.Index.Passport>();
    public async Task<PassportDto> Handle(GetPassportByIdQuery request, CancellationToken cancellationToken)
    {
        var unitOfMeasure = await _passportRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return unitOfMeasure.Adapt<PassportDto>();
    }
}
