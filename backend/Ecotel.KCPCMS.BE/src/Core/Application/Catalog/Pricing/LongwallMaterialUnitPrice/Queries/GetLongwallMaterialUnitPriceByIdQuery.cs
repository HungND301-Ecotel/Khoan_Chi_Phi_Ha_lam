using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record GetLongwallMaterialUnitPriceByIdQuery(DefaultIdType Id) : IRequest<LongwallMaterialUnitPriceDetailDto>;

public class GetLongwallMaterialUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetLongwallMaterialUnitPriceByIdQuery, LongwallMaterialUnitPriceDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice>();

    public async Task<LongwallMaterialUnitPriceDetailDto> Handle(GetLongwallMaterialUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(u => u.Code)
                .Include(u => u.ProductionProcess).ThenInclude(p => p.Code)
                .Include(u => u.LongwallParameters)
                .Include(u => u.CuttingThickness)
                .Include(u => u.SeamFace)
                .Include(u => u.Technology)
                .Include(m => m.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.AssignmentCode).ThenInclude(m => m.Code)
                .Include(m => m.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Code)
                .Include(m => m.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(m => m.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var costs = materialUnitPrice.MaterialUnitPriceAssignmentCodes
            .Select(cost =>
            {
                var unitPrice = cost.Material?.Costs
                    .FirstOrDefault(item =>
                        item.StartMonth <= materialUnitPrice.StartMonth &&
                        item.EndMonth >= materialUnitPrice.StartMonth)
                    ?.Amount ?? 0;

                return new MaterialUnitPriceAssignmentCodeDto
                {
                    AssignmentCodeId = cost.AssignmentCodeId,
                    AssignmentCode = cost.AssignmentCode?.Code?.Value ?? string.Empty,
                    AssignmentCodeName = cost.AssignmentCode?.Name ?? string.Empty,
                    MaterialId = cost.MaterialId,
                    MaterialCode = cost.Material?.Code?.Value ?? string.Empty,
                    MaterialName = cost.Material?.Name ?? string.Empty,
                    UnitOfMeasureName = cost.Material?.UnitOfMeasure?.Name ?? string.Empty,
                    UnitPrice = unitPrice,
                    Norm = cost.Norm,
                    TotalPrice = cost.TotalPrice,
                };
            })
            .OrderBy(cost => cost.AssignmentCode)
            .ThenBy(cost => cost.MaterialCode)
            .ThenBy(cost => cost.MaterialName)
            .ToList();

        return new LongwallMaterialUnitPriceDetailDto
        {
            Id = materialUnitPrice.Id,
            Code = materialUnitPrice.Code.Value,
            CuttingThickness = new CuttingThicknessDto
            {
                Id = materialUnitPrice.CuttingThickness!.Id,
                Value = materialUnitPrice.CuttingThickness.Value,
            },
            LongwallParameters = new LongwallParametersDto
            {
                Id = materialUnitPrice.LongwallParameters!.Id,
                Llc = materialUnitPrice.LongwallParameters.Llc,
                Lkc = materialUnitPrice.LongwallParameters.Lkc,
                Mk = materialUnitPrice.LongwallParameters.Mk,
            },
            SeamFaceId = materialUnitPrice.SeamFaceId,
            TechnologyId = materialUnitPrice.TechnologyId,
            PowerId = materialUnitPrice.PowerId,
            HardnessId = materialUnitPrice.HardnessId,
            IsLongwallMaterialUnitPriceCGH = materialUnitPrice.IsLongwallMaterialUnitPriceCGH,
            ProcessId = materialUnitPrice.ProcessId,
            ProcessCode = materialUnitPrice.ProductionProcess!.Code!.Value,
            StartMonth = materialUnitPrice.StartMonth,
            EndMonth = materialUnitPrice.EndMonth,
            OtherMaterialValue = materialUnitPrice.OtherMaterialvalue,
            Costs = costs
        };
    }
}
