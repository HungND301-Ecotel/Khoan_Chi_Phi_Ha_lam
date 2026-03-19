using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Part.Commands;

public record UpdatePartCommand(UpdatePartDto UpdateModel) : IRequest<bool>;

public class UpdatePartCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdatePartCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Part> _partRepository = unitOfWork.GetRepository<Domain.Entities.Index.Part>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    public async Task<bool> Handle(UpdatePartCommand request, CancellationToken cancellationToken)
    {
        var equipmentIds = request.UpdateModel.EquipmentIds.Distinct().ToList();
        if (!equipmentIds.Any())
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        var equipments = await _equipmentRepository.GetAllAsync(
            predicate: x => equipmentIds.Contains(x.Id),
            disableTracking: false);
        if (equipments.Count != equipmentIds.Count)
        {
            throw new NotFoundException(CustomResponseMessage.EquipmentNotFound);
        }

        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }
        var existedPart = await _partRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(c => c.Costs).Include(c => c.Code).Include(c => c.EquipmentParts),
            disableTracking: false) ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        if (await codeService.IsPartCodeExisted(request.UpdateModel.Code, existedPart.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            _costRepository.Delete(existedPart.Costs.ToList());
            await unitOfWork.SaveChangesAsync();

            existedPart.Update(
                request.UpdateModel.Code,
                request.UpdateModel.Name,
                request.UpdateModel.UnitOfMeasureId,
                equipments.ToList());

            var costList = new List<Cost>();
            foreach (var cost in request.UpdateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    costTypeId: existedPart.Id));
            }

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existedPart.AddCost(costList);

            _partRepository.Update(existedPart);

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
