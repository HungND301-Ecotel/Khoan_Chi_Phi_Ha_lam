using System;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;


namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Queries;

public record GetMechanizedTransportOverheadUnitPriceByIdQuery(Guid Id) : IRequest<MechanizedTransportOverheadUnitPriceDto>;

public class GetMechanizedTransportOverheadUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetMechanizedTransportOverheadUnitPriceByIdQuery, MechanizedTransportOverheadUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice>();

    public async Task<MechanizedTransportOverheadUnitPriceDto> Handle(GetMechanizedTransportOverheadUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.Id,
            include: x => x.Include(m => m.ProcessGroup),
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new MechanizedTransportOverheadUnitPriceDto
        {
            Id = entity.Id,
            ProcessGroupId = entity.ProcessGroupId,
            ProcessGroupName = entity.ProcessGroup?.Name ?? string.Empty,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            LowValuePerishableSupplyUnitPrice = entity.LowValuePerishableSupplyUnitPrice,
            ElectricityUnitPrice = entity.ElectricityUnitPrice
        };
    }
}
