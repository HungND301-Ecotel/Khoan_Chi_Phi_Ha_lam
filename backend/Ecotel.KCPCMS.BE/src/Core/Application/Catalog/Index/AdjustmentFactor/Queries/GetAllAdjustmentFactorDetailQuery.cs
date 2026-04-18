using Application.Common.Exceptions;
using Application.Common.Models;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.AdjustmentFactor.Queries;

public record GetAllAdjustmentFactorDetailQuery(Guid? ProcessGroupId = null) : IRequest<IList<AdjustmentFactorDetailDto>>;

public class GetAllAdjustmentFactorDetailQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllAdjustmentFactorDetailQuery, IList<AdjustmentFactorDetailDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.AdjustmentFactor> _adjustmentFactorRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.AdjustmentFactor>();

    public async Task<IList<AdjustmentFactorDetailDto>> Handle(GetAllAdjustmentFactorDetailQuery request,
        CancellationToken cancellationToken)
    {
        var adjustmentFactors = await _adjustmentFactorRepository.GetAllAsync(
            include: a => a
            .Include(a => a.AdjustmentFactorDescriptions)
            .Include(a => a.ProcessGroup)
            .Include(a => a.Code!),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        // Filter by ProcessGroupId if provided
        if (request.ProcessGroupId.HasValue)
        {
            adjustmentFactors = adjustmentFactors.Where(a => a.ProcessGroupId == request.ProcessGroupId).ToList();
        }

        return adjustmentFactors.Select(a => new AdjustmentFactorDetailDto
        {
            Id = a.Id,
            Code = a.Code?.Value ?? "",
            Name = a.Name,
            Type = a.Type,
            ProcessGroupId = a.ProcessGroupId,
            ProcessGroupName = a.ProcessGroup?.Name ?? "",
            AdjustmentFactorDescriptions = a.AdjustmentFactorDescriptions.Select(ad => new ShortAdjustmentFactorDescriptionDto
            {
                Id = ad.Id,
                Description = ad.Description,
                MaintenanceAdjustmentValue = ad.MaintenanceAdjustmentValue,
                ElectricityAdjustmentValue = ad.ElectricityAdjustmentValue
            }).ToList()
        }).OrderByCodeNatural(a => a.Code).ToList();
    }
}