using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AssignmentCode;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

using Application.Dto.Catalog.Cost;

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
                .Include(c => c.Costs)
                .Include(c => c.Code)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Code)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(c => c.AssignmentCodeMaterials).ThenInclude(m => m.Material).ThenInclude(m => m.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var assignmentMaterials = detail.AssignmentCodeMaterials
            .Where(link => link.Material != null)
            .Select(link => new AssignmentCodeMaterialDto
            {
                Id = link.Material!.Id,
                Code = link.Material.Code?.Value ?? string.Empty,
                Name = link.Material.Name,
                UnitOfMeasureName = link.Material.UnitOfMeasure != null ? link.Material.UnitOfMeasure.Name : string.Empty,
                MaterialType = link.Material.MaterialType,
                Role = link.Role,
                CostAmount = link.Material.Costs
                    .Where(c => c.CostType == CostType.Material && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                    .Select(c => c.Amount)
                    .FirstOrDefault(),
                ActualAmount = link.Material.Costs
                    .Where(c => c.CostType == CostType.Material && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                    .Select(c => c.ActualAmount)
                    .FirstOrDefault()
            })
            .OrderBy(m => m.Code)
            .ThenBy(m => m.Name)
            .ToList();

        return new AssignmentCodeDto
        {
            Id = detail.Id,
            Code = detail.Code?.Value ?? "",
            Name = detail.Name,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitOfMeasureName = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : string.Empty,
            CurrentPrice = detail.Costs
                .Where(c => c.CostType == CostType.Electricity && c.StartMonth <= checkDate && c.EndMonth >= checkDate)
                .Select(c => c.Amount)
                .FirstOrDefault(),
            Costs = detail.Costs
                .Where(c => c.CostType == CostType.Electricity)
                .Select(c => new ElectricityCostDto
                {
                    StartMonth = c.StartMonth,
                    EndMonth = c.EndMonth,
                    Amount = c.Amount,
                })
                .OrderBy(c => c.StartMonth)
                .ThenBy(c => c.EndMonth)
                .ToList(),
            Materials = assignmentMaterials
                .Where(m => m.Role == AssignmentCodeMaterialRole.Material)
                .ToList(),
            OtherMaterials = assignmentMaterials
                .Where(m => m.Role == AssignmentCodeMaterialRole.OtherMaterial)
                .ToList()
        };
    }
}
