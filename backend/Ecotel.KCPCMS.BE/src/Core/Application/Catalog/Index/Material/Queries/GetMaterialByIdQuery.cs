using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Cost;
using Application.Dto.Catalog.Material;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Material.Queries;

public record GetMaterialByIdQuery(DefaultIdType Id) : IRequest<MaterialDetailDto>;

public class GetMaterialByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMaterialByIdQuery, MaterialDetailDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    public async Task<MaterialDetailDto> Handle(GetMaterialByIdQuery request, CancellationToken cancellationToken)
    {
        var details = await _materialRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t
                .Include(t => t.AssignmentCode).ThenInclude(t => t.Code)
                .Include(t => t.UnitOfMeasure)
                .Include(t => t.Costs)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


        return new MaterialDetailDto
        {
            Id = details.Id,
            Code = details.Code.Value,
            Name = details.Name,
            AssigmentCodeId = details.AssigmentCodeId,
            UnitOfMeasureId = details.UnitOfMeasureId,
            UsageTime = details.UsageTime,
            AssigmentCode = details.AssignmentCode is { Code: not null } ? details.AssignmentCode.Code.Value : string.Empty,
            UnitOfMeasureName = details.AssignmentCode != null ? details.AssignmentCode.Name : string.Empty,
            Costs = details.Costs.Adapt<List<MaterialCostDto>>(),
            MaterialType = details.MaterialType
        };
    }
}
