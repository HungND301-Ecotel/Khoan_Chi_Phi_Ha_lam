using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using Application.Dto.Catalog.LongwallParameters;
using Application.Dto.Catalog.MaterialUnitPrice;
using Mapster;
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
                .Include(m => m.MaterialUnitPriceAssignmentCodes).ThenInclude(m => m.AssignmentCode),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new LongwallMaterialUnitPriceDetailDto
        {
            Id = materialUnitPrice.Id,
            Code = materialUnitPrice.Code.Value,
            CuttingThickness = materialUnitPrice.CuttingThickness.Adapt<CuttingThicknessDto>(),
            LongwallParameters = materialUnitPrice.LongwallParameters.Adapt<LongwallParametersDto>(),
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
            Costs = materialUnitPrice.MaterialUnitPriceAssignmentCodes.Adapt<List<MaterialUnitPriceAssignmentCodeDto>>()
        };
    }
}
