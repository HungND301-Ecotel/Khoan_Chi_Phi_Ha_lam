using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Common.Enums;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;

public record GetMaterialUnitPriceByIdQuery(DefaultIdType Id) : IRequest<MaterialUnitPriceDetailDto>;

public class GetMaterialUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMaterialUnitPriceByIdQuery, MaterialUnitPriceDetailDto>
{
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    public async Task<MaterialUnitPriceDetailDto> Handle(GetMaterialUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var materialUnitPrice = await _materialUnitPriceRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(u => u.Code)
                .Include(u => u.Passport)
                .Include(u => u.Hardness)
                .Include(u => u.InsertItem)
                .Include(u => u.SupportStep)
                .Include(u => u.ProductionProcess)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.AssignmentCode).ThenInclude(m => m.Code)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Code)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.Material).ThenInclude(m => m.Costs),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        string passportName =
            $"H/c {materialUnitPrice.Passport!.Name}; {materialUnitPrice.Passport!.Sd}; {materialUnitPrice.Passport!.Sc}";
        return new MaterialUnitPriceDetailDto
        {
            Id = materialUnitPrice.Id,
            Code = materialUnitPrice.Code.Value,
            Name = $"{materialUnitPrice.ProductionProcess!.Name}, {passportName}, {materialUnitPrice.InsertItem!.Value}, {materialUnitPrice.SupportStep!.Value}, {materialUnitPrice.Hardness!.Value}",
            StartMonth = materialUnitPrice.StartMonth,
            EndMonth = materialUnitPrice.EndMonth,
            HardnessId = materialUnitPrice.HardnessId,
            InsertItemId = materialUnitPrice.InsertItemId,
            PassportId = materialUnitPrice.PassportId,
            ProcessId = materialUnitPrice.ProcessId,
            SupportStepId = materialUnitPrice.SupportStepId,
            TotalPrice = materialUnitPrice.TotalPrice,
            OtherMaterialValue = materialUnitPrice.OtherMaterialvalue,
            Type = materialUnitPrice.Type,
            Costs = materialUnitPrice.MaterialUnitPriceAssignmentCodes
                .OrderBy(m => m.AssignmentCode.Code.Value)
                .ThenBy(m => m.AssignmentCode.Name)
                .ThenBy(m => m.Material != null ? m.Material.Code!.Value : string.Empty)
                .ThenBy(m => m.Material != null ? m.Material.Name : string.Empty)
                .Select(m => new MaterialUnitPriceAssignmentCodeDto
                {
                    AssignmentCodeId = m.AssignmentCodeId,
                    AssignmentCode = m.AssignmentCode.Code.Value,
                    AssignmentCodeName = m.AssignmentCode.Name,
                    MaterialId = m.MaterialId,
                    MaterialCode = m.Material?.Code?.Value ?? string.Empty,
                    MaterialName = m.Material?.Name ?? string.Empty,
                    UnitOfMeasureName = m.Material?.UnitOfMeasure?.Name ?? string.Empty,
                    UnitPrice = m.Material?.Costs
                        .Where(c => c.CostType == CostType.Material &&
                                    c.StartMonth <= materialUnitPrice.StartMonth &&
                                    c.EndMonth >= materialUnitPrice.StartMonth)
                        .Select(c => c.Amount)
                        .FirstOrDefault() ?? 0,
                    Norm = m.Norm,
                    TotalPrice = m.TotalPrice
                })
                .ToList()
        };
    }
}
