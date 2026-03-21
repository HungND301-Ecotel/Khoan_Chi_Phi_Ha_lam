using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOrder;
using MediatR;

namespace Application.Catalog.Index.ProductionOrder.Commands;

public record CreateProductionOrderCommand(CreateProductionOrderDto CreateModel) : IRequest<bool>;

public class CreateProductionOrderCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateProductionOrderCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionOrder>();
    public async Task<bool> Handle(CreateProductionOrderCommand request, CancellationToken cancellationToken)
    {
        var newProductionOrder = Domain.Entities.Index.ProductionOrder.Create(request.CreateModel.Code, request.CreateModel.Name, request.CreateModel.StartMonth, request.CreateModel.EndMonth);
        await _productionOrderRepository.InsertAsync(newProductionOrder);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
