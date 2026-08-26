using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Queries;

public record GetProductionOutputByIdQuery(DefaultIdType Id) : IRequest<ProductionOutputDto>;

public class GetProductionOutputByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductionOutputByIdQuery, ProductionOutputDto>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();

    public async Task<ProductionOutputDto> Handle(GetProductionOutputByIdQuery request, CancellationToken cancellationToken)
    {
        var productionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: q => q
                .Include(p => p.AcceptanceReport)
                .Include(p => p.Department)
                    .ThenInclude(d => d.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputProducts)
                        .ThenInclude(pp => pp.Product)
                            .ThenInclude(pr => pr.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.ProductionProcess)
                            .ThenInclude(pp => pp!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.Equipment)
                            .ThenInclude(e => e!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.TransportRoute)
                            .ThenInclude(r => r!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.RouteDepartment)
                            .ThenInclude(d => d!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.HaulDistance)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.CargoType)
                            .ThenInclude(c => c!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.ReceivingLocation)
                            .ThenInclude(l => l!.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputTransportLines)
                        .ThenInclude(tl => tl.DumpingLocation)
                            .ThenInclude(l => l!.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ProductionOutputDto
        {
            Id = productionOutput.Id,
            StartMonth = productionOutput.StartMonth,
            EndMonth = productionOutput.EndMonth,
            DepartmentId = productionOutput.DepartmentId,
            DepartmentCode = productionOutput.Department?.Code?.Value ?? string.Empty,
            DepartmentName = productionOutput.Department?.Name ?? string.Empty,
            AcceptanceReportId = productionOutput.AcceptanceReport?.Id,
            ProductionMeters = productionOutput.ProductionMeters,
            StandardProductionMeters = productionOutput.StandardProductionMeters,
            ProcessGroups = productionOutput.ProductionOutputProcessGroups
                .Select(g => new ProductionOutputProcessGroupDto
                {
                    ProcessGroupId = g.ProcessGroupId,
                    ProcessGroupCode = g.ProcessGroup?.Code?.Value
                        ?? g.ProcessGroup?.FixedKey?.Key
                        ?? string.Empty,
                    ProcessGroupName = g.ProcessGroup?.Name ?? string.Empty,
                    PlanProductionMeters = g.PlanProductionMeters,
                    StandardProductionMeters = g.StandardProductionMeters,
                    ProductionMeters = g.ProductionMeters,
                    Products = g.ProductionOutputProducts
                        .Select(p => new ProductionOutputProductDto
                        {
                            ProductId = p.ProductId,
                            ProductCode = p.Product?.Code?.Value ?? string.Empty,
                            ProductName = p.Product?.Name ?? string.Empty,
                            ProductionMeters = p.ProductionMeters,
                            ActualAshContent = p.ActualAshContent
                        }).ToList(),
                    TransportLines = g.ProductionOutputTransportLines
                        .Select(tl => new ProductionOutputTransportLineDto
                        {
                            ProductionProcessId = tl.ProductionProcessId,
                            ProductionProcessCode = tl.ProductionProcess?.Code?.Value ?? string.Empty,
                            ProductionProcessName = tl.ProductionProcess?.Name ?? string.Empty,
                            EquipmentId = tl.EquipmentId,
                            EquipmentCode = tl.Equipment?.Code?.Value,
                            EquipmentName = tl.Equipment?.Name,
                            EquipmentQuality = tl.EquipmentQuality,
                            TransportRouteId = tl.TransportRouteId,
                            TransportRouteCode = tl.TransportRoute?.Code?.Value,
                            TransportRouteName = tl.TransportRoute?.Name,
                            RouteDepartmentId = tl.RouteDepartmentId,
                            RouteDepartmentCode = tl.RouteDepartment?.Code?.Value,
                            RouteDepartmentName = tl.RouteDepartment?.Name,
                            HaulDistanceId = tl.HaulDistanceId,
                            HaulDistanceValue = tl.HaulDistance?.Value,
                            CargoTypeId = tl.CargoTypeId,
                            CargoTypeName = tl.CargoType?.Name,
                            ReceivingLocationId = tl.ReceivingLocationId,
                            ReceivingLocationName = tl.ReceivingLocation?.Name,
                            DumpingLocationId = tl.DumpingLocationId,
                            DumpingLocationName = tl.DumpingLocation?.Name,
                            ProductionMeters = tl.ProductionMeters
                        }).ToList()
                }).ToList()
        };
    }
}
