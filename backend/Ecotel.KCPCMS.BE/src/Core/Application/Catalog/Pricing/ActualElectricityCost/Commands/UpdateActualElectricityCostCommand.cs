using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ActualElectricityCost;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.ActualElectricityCost.Commands;

public record UpdateActualElectricityCostCommand(UpdateActualElectricityCostDto UpdateModel) : IRequest<bool>;

public class UpdateActualElectricityCostCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateActualElectricityCostCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.ActualElectricityCost> _actualElectricityCostRepository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.ActualElectricityCost>();
    private readonly IWriteRepository<ActualEletricityEquipment> _actualEletricityEquipmentRepository =
        unitOfWork.GetRepository<ActualEletricityEquipment>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository =
        unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository =
        unitOfWork.GetRepository<AssignmentCode>();

    public async Task<bool> Handle(UpdateActualElectricityCostCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdateModel.Equipments == null || !request.UpdateModel.Equipments.Any())
        {
            throw new BadRequestException(CustomResponseMessage.CostsCannotBeEmpty);
        }

        var model = await _actualElectricityCostRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.UpdateModel.Id,
            include: x => x.Include(c => c.ActualEletricityEquipment))
            ?? throw new NotFoundException(CustomResponseMessage.ActualElectricityCostNotFound);

        bool hasAcceptanceReport = await _acceptanceReportRepository.ExistsAsync(
            x => x.Id == request.UpdateModel.AcceptanceReportId);
        if (!hasAcceptanceReport)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var assignmentCodeIds = request.UpdateModel.Equipments.Select(x => x.EquipmentId).Distinct().ToList();
        int assignmentCodeCount = await _assignmentCodeRepository.CountAsync(x => assignmentCodeIds.Contains(x.Id));
        if (assignmentCodeCount != assignmentCodeIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        var newItems = request.UpdateModel.Equipments
            .Select(x => ActualEletricityEquipment.Create(Guid.Empty, x.EquipmentId, x.ActualElectricityConsumption))
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _actualEletricityEquipmentRepository.Delete(model.ActualEletricityEquipment.ToList());

            model.ClearActualEletricityEquipments();
            model.Update(request.UpdateModel.AcceptanceReportId);
            model.AddActualEletricityEquipments(newItems);

            _actualElectricityCostRepository.Update(model);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }

        return true;
    }
}
