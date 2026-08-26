using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportPlanLine;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using TransportPlanLineEntity = Domain.Entities.Pricing.TransportPlanLine;

namespace Application.Catalog.Pricing.TransportPlanLine.Queries;

public record GetTransportPlanLineByIdQuery(Guid Id) : IRequest<TransportPlanLineItemDto>;

public class GetTransportPlanLineByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetTransportPlanLineByIdQuery, TransportPlanLineItemDto>
{
    private readonly IWriteRepository<TransportPlanLineEntity> _transportPlanLineRepository =
        unitOfWork.GetRepository<TransportPlanLineEntity>();

    public async Task<TransportPlanLineItemDto> Handle(
        GetTransportPlanLineByIdQuery request,
        CancellationToken cancellationToken)
    {
        var line = await _transportPlanLineRepository.GetAll()
            .Where(x => x.Id == request.Id)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.Code)
            .Include(x => x.ProductionProcess)
                .ThenInclude(p => p!.ProcessGroup)
                    .ThenInclude(pg => pg!.FixedKey)
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
                    .ThenInclude(f => f.AdjustmentFactor)
                        .ThenInclude(a => a!.Code)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactorDescription)
                        .ThenInclude(d => d!.AdjustmentFactor)
                            .ThenInclude(a => a.FixedKey)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.AdjustmentFactors)
                    .ThenInclude(f => f.AdjustmentFactorDescription)
                        .ThenInclude(d => d!.AdjustmentFactor)
                            .ThenInclude(a => a.Code)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.TransportUnitPrice)
            .Include(x => x.PlannedTransportCost)
                .ThenInclude(c => c!.MechanizedTransportUnitPriceDetail)
            .Include(x => x.HaulDistance)
            .Include(x => x.CargoType)
                .ThenInclude(c => c!.Code)
            .Include(x => x.ReceivingLocation)
                .ThenInclude(l => l!.Code)
            .Include(x => x.DumpingLocation)
                .ThenInclude(l => l!.Code)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (line == null)
        {
            throw new NotFoundException(CustomResponseMessage.TransportPlanLineNotFound);
        }

        return GetTransportPlanLineByDepartmentQueryHandler.ToItemDto(line);
    }
}
