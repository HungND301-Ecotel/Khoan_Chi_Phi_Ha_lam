using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOrder;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Queries;

public record GetProductionOrderByIdQuery(DefaultIdType Id) : IRequest<ProductionOrderDto>;

public class GetProductionOrderByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductionOrderByIdQuery, ProductionOrderDto>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionOrder>();
    public async Task<ProductionOrderDto> Handle(GetProductionOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _productionOrderRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.Id,
            include: t => t.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        return new ProductionOrderDto
        {
            Id = entity.Id,
            Code = entity.Code.Value,
            Name = entity.Name,
            StartMonth = entity.StartMonth,
            EndMonth = entity.EndMonth
        };
    }
}
