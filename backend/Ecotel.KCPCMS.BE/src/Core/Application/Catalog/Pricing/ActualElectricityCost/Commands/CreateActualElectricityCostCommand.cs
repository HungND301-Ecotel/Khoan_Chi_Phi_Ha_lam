using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ActualElectricityCost;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Production;
using MediatR;
using Shared.Constants;

namespace Application.Catalog.Pricing.ActualElectricityCost.Commands;

public record CreateActualElectricityCostCommand(CreateActualElectricityCostDto CreateModel) : IRequest<bool>;

public class CreateActualElectricityCostCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateActualElectricityCostCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ActualElectricityCost> _actualElectricityCostRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ActualElectricityCost>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository =
        unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<Equipment> _equipmentRepository =
        unitOfWork.GetRepository<Equipment>();

    public async Task<bool> Handle(CreateActualElectricityCostCommand request, CancellationToken cancellationToken)
    {
        if (request.CreateModel.Equipments == null || !request.CreateModel.Equipments.Any())
        {
            throw new BadRequestException(CustomResponseMessage.CostsCannotBeEmpty);
        }

        bool hasAcceptanceReport = await _acceptanceReportRepository.ExistsAsync(
            x => x.Id == request.CreateModel.AcceptanceReportId);
        if (!hasAcceptanceReport)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        bool existed = await _actualElectricityCostRepository.ExistsAsync(
            x => x.AcceptanceReportId == request.CreateModel.AcceptanceReportId);
        if (existed)
        {
            throw new BadRequestException(CustomResponseMessage.InvalidParams);
        }

        var equipmentIds = request.CreateModel.Equipments.Select(x => x.EquipmentId).Distinct().ToList();
        int equipmentCount = await _equipmentRepository.CountAsync(x => equipmentIds.Contains(x.Id));
        if (equipmentCount != equipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var equipments = request.CreateModel.Equipments
            .Select(x => ActualEletricityEquipment.Create(Guid.Empty, x.EquipmentId, x.ActualElectricityConsumption))
            .ToList();

        var model = Domain.Entities.Pricing.ActualElectricityCost.Create(request.CreateModel.AcceptanceReportId, equipments);
        await _actualElectricityCostRepository.InsertAsync(model, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return true;
    }
}
