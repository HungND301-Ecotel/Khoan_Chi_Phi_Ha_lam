using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record GetAllAcceptanceReportAdditionalCostQuery(Guid AcceptanceReportId) : IRequest<GetAllAcceptanceReportAdditionalCostResponseDto>;

public class GetAllAcceptanceReportAdditionalCostQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllAcceptanceReportAdditionalCostQuery, GetAllAcceptanceReportAdditionalCostResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<GetAllAcceptanceReportAdditionalCostResponseDto> Handle(GetAllAcceptanceReportAdditionalCostQuery request, CancellationToken cancellationToken)
    {
        // Get AcceptanceReport with items
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == request.AcceptanceReportId,
            include: q => q
                .Include(a => a.AcceptanceReportItems)
                .ThenInclude(i => i.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                .ThenInclude(i => i.Material).ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems)
                .ThenInclude(i => i.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                .ThenInclude(i => i.Part).ThenInclude(p => p.Code),
            disableTracking: true);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Filter items with AdditionalCost != None
        var itemsWithAdditionalCost = acceptanceReport.AcceptanceReportItems
            .Where(i => i.AdditionalCost != AdditionalCost.None)
            .ToList();

        // Group by AdditionalCost type
        var additionalCostsGroup = new AdditionalCostsGroupDto();

        foreach (var item in itemsWithAdditionalCost)
        {
            var materialId = item.TrackedMaterialId;
            var code = item.Material?.Code?.Value ?? item.Part?.Code?.Value;
            var name = item.Material?.Name ?? item.Part?.Name;

            var costItem = new AdditionalCostItemDto
            {
                MaterialId = materialId,
                Code = code,
                Name = name,
                UnitOfMeasureName = item.Material?.UnitOfMeasure?.Name
                    ?? item?.Part?.UnitOfMeasure?.Name,
                AdditionalCostQuantity = item.AdditionalCostQuantity
            };

            // Group based on AdditionalCost type
            switch (item.AdditionalCost)
            {
                case AdditionalCost.Material:
                    additionalCostsGroup.Material.Add(costItem);
                    break;
                case AdditionalCost.Maintain:
                    additionalCostsGroup.Maintain.Add(costItem);
                    break;
                case AdditionalCost.SafeAndWelfare:
                    additionalCostsGroup.OtherMaterial.Add(costItem);
                    break;
            }
        }

        return new GetAllAcceptanceReportAdditionalCostResponseDto
        {
            Id = acceptanceReport.Id,
            AdditionalCosts = additionalCostsGroup
        };
    }
}

