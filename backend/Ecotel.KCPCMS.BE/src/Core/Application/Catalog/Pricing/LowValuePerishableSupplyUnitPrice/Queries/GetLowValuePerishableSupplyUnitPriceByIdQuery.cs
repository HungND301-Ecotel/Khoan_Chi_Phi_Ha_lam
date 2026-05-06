using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Domain.Common.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Queries;

public record GetLowValuePerishableSupplyUnitPriceByIdQuery(DefaultIdType Id, LowValuePerishableSupplyType Type) : IRequest<LowValuePerishableSupplyUnitPriceDto>;

public class GetLowValuePerishableSupplyUnitPriceByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetLowValuePerishableSupplyUnitPriceByIdQuery, LowValuePerishableSupplyUnitPriceDto>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();

    public async Task<LowValuePerishableSupplyUnitPriceDto> Handle(GetLowValuePerishableSupplyUnitPriceByIdQuery request, CancellationToken cancellationToken)
    {
        Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice entity = await _repository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id && e.Type == request.Type,
            include: e => e.Include(x => x.Department).ThenInclude(d => d!.Code)
                .Include(x => x.ProcessGroup).ThenInclude(pg => pg!.FixedKey),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceNotFound);

        return new LowValuePerishableSupplyUnitPriceDto
        {
            Id = entity.Id,
            DepartmentId = entity.DepartmentId,
            DepartmentCode = entity.Department?.Code?.Value ?? string.Empty,
            DepartmentName = entity.Department?.Name ?? string.Empty,
            ProcessGroupId = entity.ProcessGroupId,
            ProcessGroupCode = entity.ProcessGroup?.FixedKey?.Key ?? string.Empty,
            ProcessGroupName = entity.ProcessGroup?.Name ?? string.Empty,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth,
            Type = entity.Type,
            TotalPrice = entity.TotalPrice,
        };
    }
}