using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.TransportPlanLine.Queries;

public record GetAllTransportPlanLineQuery(
    bool IgnorePagination = true,
    ProductUnitPriceScenarioType ScenarioType = ProductUnitPriceScenarioType.Plan
) : IRequest<List<TransportPlanLineByDepartmentDetailDto>>;

public class GetAllTransportPlanLineQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllTransportPlanLineQuery, List<TransportPlanLineByDepartmentDetailDto>>
{
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();

    public async Task<List<TransportPlanLineByDepartmentDetailDto>> Handle(
        GetAllTransportPlanLineQuery request,
        CancellationToken cancellationToken)
    {
        var lines = await _transportPlanLineRepository.GetAll()
            .Where(x =>
                x.ScenarioType == request.ScenarioType &&
                x.OutputType == OutputType.PlanOutput)
            .Include(x => x.Department)
                .ThenInclude(d => d!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.Code)
            .Include(x => x.UnitOfMeasure)
            .Include(x => x.Equipment)
                .ThenInclude(e => e!.Code)
            .Include(x => x.TransportRoute)
                .ThenInclude(r => r!.Code)
            .Include(x => x.RouteDepartment)
                .ThenInclude(rd => rd!.Code)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactor)
                        .ThenInclude(a => a!.FixedKey)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactorDescription)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = lines
            .GroupBy(x => x.DepartmentId)
            .Select(deptGroup =>
            {
                var department = deptGroup.First();
                return new TransportPlanLineByDepartmentDetailDto
                {
                    DepartmentId = deptGroup.Key,
                    DepartmentCode = department.Department?.Code?.Value ?? string.Empty,
                    DepartmentName = department.Department?.Name ?? string.Empty,
                    Months = deptGroup
                        .GroupBy(x => x.StartMonth)
                        .OrderBy(x => x.Key)
                        .Select(monthGroup => new TransportPlanLineMonthDto
                        {
                            Month = monthGroup.Key,
                            Items = monthGroup
                                .OrderBy(x => x.ProductionProcess?.Code?.Value ?? string.Empty)
                                .Select(GetTransportPlanLineByDepartmentQueryHandler.ToItemDto)
                                .ToList(),
                        })
                        .ToList(),
                };
            })
            .OrderBy(x => x.DepartmentCode)
            .ToList();

        return result;
    }
}
