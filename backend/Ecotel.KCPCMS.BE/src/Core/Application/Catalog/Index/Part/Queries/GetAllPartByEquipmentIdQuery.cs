using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Queries;

public record GetAllPartByEquipmentIdQuery(DefaultIdType EquipmentId) : IRequest<IList<PartDetailBaseDto>>;

public class GetAllPartByEquipmentIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllPartByEquipmentIdQuery, IList<PartDetailBaseDto>>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    public async Task<IList<PartDetailBaseDto>> Handle(GetAllPartByEquipmentIdQuery request, CancellationToken cancellationToken)
    {
        var details = await _partRepository.GetAllAsync(
            predicate: t => t.EquipmentId == request.EquipmentId,
            include: t => t
                .Include(t => t.UnitOfMeasure)
                .Include(t => t.Costs)
                .Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var curMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return details.Select(partDetail => new PartDetailBaseDto
        {
            Id = partDetail.Id,
            Code = partDetail.Code.Value,
            Name = partDetail.Name,
            EquipmentId = partDetail.EquipmentId,
            UnitOfMeasureId = partDetail.UnitOfMeasureId,
            UnitOfMeasureName = partDetail.UnitOfMeasure != null ? partDetail.UnitOfMeasure.Name : string.Empty,
            CurrentCost = partDetail.Costs.FirstOrDefault(c => c.StartMonth <= curMonth && c.EndMonth >= curMonth)?.Amount ?? 0
        }).ToList();
    }
}
