using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOrder;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Passport.Commands;

public record UpdateProductionOrderCommand(ProductionOrderDto UpdateModel) : IRequest<bool>;

public class UpdateProductionOrderCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductionOrderCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionOrder>();
    public async Task<bool> Handle(UpdateProductionOrderCommand request, CancellationToken cancellationToken)
    {
        var existProductionOrder = await _productionOrderRepository.GetFirstOrDefaultAsync(
            predicate: t => t.Id == request.UpdateModel.Id,
            include: t => t.Include(t => t.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        existProductionOrder.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.StartMonth, request.UpdateModel.EndMonth);

        _productionOrderRepository.Update(existProductionOrder);
        await unitOfWork.SaveChangesAsync();
        return true;
    }
}
