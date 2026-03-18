using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Domain.Common.Enums;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record DeleteTunnelElectricityUnitPriceEquipmentCommand(Guid Id) : IRequest<bool>;

public class DeleteTunnelElectricityUnitPriceEquipmentCommandHandler(
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<DeleteTunnelElectricityUnitPriceEquipmentCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.EletricityUnitPrice.ElectricityUnitPriceEquipment>();
    private const string CacheSignalKey = "ProductUnitPrice";

    public async Task<bool> Handle(DeleteTunnelElectricityUnitPriceEquipmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetFirstOrDefaultAsync(
            predicate: e => e.Id == request.Id && e.ElectricityType == ElectricityUnitPriceType.TunnelExcavation,
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.ElectricityUnitPriceEquipmentNotFound);

        if (entity is not TunnelElectricityUnitPriceEquipment)
        {
            throw new BadRequestException("The entity is not a Tunnel Electricity Unit Price Equipment.");
        }

        _repository.Delete(entity);
        await unitOfWork.SaveChangesAsync();

        cacheService.InvalidateGroup(CacheSignalKey);

        return true;
    }
}
