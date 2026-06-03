using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Queries;

public record GetTunnelSupportAndDrillingMaterialUnitPriceByIdQuery(DefaultIdType Id) : IRequest<TunnelSupportAndDrillingMaterialUnitPriceDetailDto>;

public class GetTunnelSupportAndDrillingMaterialUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetTunnelSupportAndDrillingMaterialUnitPriceByIdQuery, TunnelSupportAndDrillingMaterialUnitPriceDetailDto>
{
    private readonly IWriteRepository<TunnelSupportAndDrillingMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelSupportAndDrillingMaterialUnitPrice>();
    public async Task<TunnelSupportAndDrillingMaterialUnitPriceDetailDto> Handle(GetTunnelSupportAndDrillingMaterialUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(u => u.Code)
                .Include(u => u.Passport)
                .Include(u => u.Hardness)
                .Include(u => u.ProductionProcess)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.AssignmentCode).ThenInclude(m => m.Code)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Code)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        string passportName =
            $"H/c {materialUnitPrice.Passport!.Name}; {materialUnitPrice.Passport!.Sd}; {materialUnitPrice.Passport!.Sc}";
        return new TunnelSupportAndDrillingMaterialUnitPriceDetailDto
        {
            Id = materialUnitPrice.Id,
            Code = materialUnitPrice.Code.Value,
            Name = $"{materialUnitPrice.ProductionProcess!.Name}, {passportName}, {materialUnitPrice.Hardness!.Value}",
            StartMonth = materialUnitPrice.StartMonth,
            EndMonth = materialUnitPrice.EndMonth,
            HardnessId = materialUnitPrice.HardnessId,
            PassportId = materialUnitPrice.PassportId,
            ProcessId = materialUnitPrice.ProcessId,
            TechnologyId = materialUnitPrice.TechnologyId,
            TotalPrice = materialUnitPrice.TotalPrice,
            OtherMaterialValue = materialUnitPrice.OtherMaterialvalue,
            Costs = materialUnitPrice.MaterialUnitPriceAssignmentCodes
                .Select(m => new MaterialUnitPriceAssignmentCodeDto
                {
                    AssignmentCodeId = m.AssignmentCodeId,
                    AssignmentCode = m.AssignmentCode?.Code?.Value ?? string.Empty,
                    AssignmentCodeName = m.AssignmentCode?.Name ?? string.Empty,
                    MaterialId = m.MaterialId,
                    MaterialCode = m.Material?.Code?.Value ?? string.Empty,
                    MaterialName = m.Material?.Name ?? string.Empty,
                    UnitOfMeasureName = m.Material?.UnitOfMeasure?.Name ?? string.Empty,
                    UnitPrice = m.Material != null ? m.Material.GetMaterialCost(materialUnitPrice.StartMonth) : 0,
                    Norm = m.Norm,
                    TotalPrice = m.TotalPrice
                })
                .OrderBy(m => m.AssignmentCode)
                .ThenBy(m => m.MaterialCode)
                .ThenBy(m => m.MaterialName)
                .ToList()
        };
    }
}
