using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AssignmentCodes.Queries;
public record GetAssignmentCodeDetailByIdQuery(DefaultIdType Id) : IRequest<AssignmentCodeDto>;

public class GetAssignmentCodeDetailByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAssignmentCodeDetailByIdQuery, AssignmentCodeDto>
{
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    public async Task<AssignmentCodeDto> Handle(GetAssignmentCodeDetailByIdQuery request, CancellationToken cancellationToken)
    {
        var detail = await _assignmentCodeRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(c => c.UnitOfMeasure).Include(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


        return new AssignmentCodeDto
        {
            Id = detail.Id,
            Code = detail.Code?.Value ?? "",
            Name = detail.Name,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitOfMeasureName = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : string.Empty
        };
    }
}
