using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;
public record UpdateEquipmentsCommand(UpdateEquipmentDto UpdateModel) : IRequest<bool>;

public class UpdateEquipmentsCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService, ICostService costService) : IRequestHandler<UpdateEquipmentsCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    public async Task<bool> Handle(UpdateEquipmentsCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        var existedEquipment = await _equipmentRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(c => c.Costs).Include(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existedEquipment.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.EquipmentCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            _costRepository.Delete(existedEquipment.Costs.ToList());
            await unitOfWork.SaveChangesAsync();

            existedEquipment.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.UnitOfMeasureId);

            var costList = new List<Cost>();
            foreach (var cost in request.UpdateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: existedEquipment.Id));
            }

            if (await costService.IsOverlap(costList))
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existedEquipment.AddCost(costList);

            _equipmentRepository.Update(existedEquipment);
            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync();
        }
        catch
        {
            await unitOfWork.RollbackAsync();
            throw;
        }

        return true;
    }
}
