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
                            .ThenInclude(pr => pr.Code),
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
                    ProcessGroupCode = g.ProcessGroup?.Code?.Value ?? string.Empty,
                    ProcessGroupName = g.ProcessGroup?.Name ?? string.Empty,
                    StandardProductionMeters = g.StandardProductionMeters,
                    ProductionMeters = g.ProductionMeters,
                    Products = g.ProductionOutputProducts
                        .Select(p => new ProductionOutputProductDto
                        {
                            ProductId = p.ProductId,
                            ProductCode = p.Product?.Code?.Value ?? string.Empty,
                            ProductName = p.Product?.Name ?? string.Empty,
                            ProductionMeters = p.ProductionMeters
                        }).ToList()
                }).ToList()
        };
    }
}
