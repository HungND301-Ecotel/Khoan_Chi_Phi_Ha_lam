using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Mapster;
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
                .Include(u => u.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.AssignmentCode),
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
            Costs = materialUnitPrice.MaterialUnitPriceAssignmentCodes.Adapt<List<MaterialUnitPriceAssignmentCodeDto>>()
        };
    }
}
