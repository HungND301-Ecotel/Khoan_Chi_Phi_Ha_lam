using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Queries;
public record GetAdjustmentFactorDescriptionByIdQuery(DefaultIdType Id) : IRequest<AdjustmentFactorDescriptionDto>;

public class GetAdjustmentFactorByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAdjustmentFactorDescriptionByIdQuery, AdjustmentFactorDescriptionDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactorDescription> _adjustmentFactorDescriptionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactorDescription>();

    public async Task<AdjustmentFactorDescriptionDto> Handle(GetAdjustmentFactorDescriptionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var adjustmentFactorDescription = await _adjustmentFactorDescriptionRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(ad => ad.AdjustmentFactor).ThenInclude(af => af.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        return new AdjustmentFactorDescriptionDto
        {
            Id = adjustmentFactorDescription.Id,
            AdjustmentFactorId = adjustmentFactorDescription.AdjustmentFactorId,
            Description = adjustmentFactorDescription.Description,
            AdjustmentFactorCode = adjustmentFactorDescription.AdjustmentFactor.Code?.Value ?? "",
            ElectricityAdjustmentValue = adjustmentFactorDescription.ElectricityAdjustmentValue,
            MaintenanceAdjustmentValue = adjustmentFactorDescription.MaintenanceAdjustmentValue,
        };
    }
}
