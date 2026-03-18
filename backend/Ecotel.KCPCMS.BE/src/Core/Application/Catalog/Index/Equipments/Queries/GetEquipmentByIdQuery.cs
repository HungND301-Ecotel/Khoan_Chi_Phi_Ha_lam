using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Cost;
using Application.Dto.Catalog.Equipment;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Queries;
public record GetEquipmentByIdQuery(DefaultIdType Id) : IRequest<EquipmentDetailDto>;

public class GetEquipmentByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetEquipmentByIdQuery, EquipmentDetailDto>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    public async Task<EquipmentDetailDto> Handle(GetEquipmentByIdQuery request, CancellationToken cancellationToken)
    {
        var detail = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(c => c.UnitOfMeasure).Include(c => c.Costs).Include(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);


        return new EquipmentDetailDto
        {
            Id = detail.Id,
            Code = detail.Code?.Value ?? "",
            Name = detail.Name,
            UnitOfMeasureId = detail.UnitOfMeasureId,
            UnitOfMeasureName = detail.UnitOfMeasure != null ? detail.UnitOfMeasure.Name : string.Empty,
            Costs = detail.Costs.Adapt<List<ElectricityCostDto>>()
        };
    }
}
