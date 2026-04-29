using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Domain.Common.Enums;
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
        var now = DateTime.UtcNow;
        var checkDate = new DateOnly(now.Year, now.Month, 1);

        var detail = await _assignmentCodeRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(c => c.UnitOfMeasure)
                .Include(c => c.Code)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Code)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


        return new AssignmentCodeDto
        {
            Id = detail.Id,
            Code = detail.Code?.Value ?? "",
            Name = detail.Name,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitOfMeasureName = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : string.Empty,
            Materials = detail.AssignmentCodeMaterials
                .Where(link => link.Material != null)
                .Select(link => link.Material!)
                .Select(m => new AssignmentCodeMaterialDto
                {
                    Id = m.Id,
                    Code = m.Code?.Value ?? string.Empty,
                    Name = m.Name,
                    UnitOfMeasureName = m.UnitOfMeasure != null ? m.UnitOfMeasure.Name : string.Empty,
                    CostAmount = m.Costs
                        .Where(c => c.CostType == CostType.Material && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                        .Select(c => c.Amount)
                        .FirstOrDefault(),
                    ActualAmount = m.Costs
                        .Where(c => c.CostType == CostType.Material && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                        .Select(c => c.ActualAmount)
                        .FirstOrDefault()
                })
                .OrderBy(m => m.Code)
                .ThenBy(m => m.Name)
                .ToList()
        };
    }
}
