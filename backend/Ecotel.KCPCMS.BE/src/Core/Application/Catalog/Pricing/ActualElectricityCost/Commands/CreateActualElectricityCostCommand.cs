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
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository =
        unitOfWork.GetRepository<AssignmentCode>();

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

        var assignmentCodeIds = request.CreateModel.Equipments.Select(x => x.EquipmentId).Distinct().ToList();
        int assignmentCodeCount = await _assignmentCodeRepository.CountAsync(x => assignmentCodeIds.Contains(x.Id));
        if (assignmentCodeCount != assignmentCodeIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
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
