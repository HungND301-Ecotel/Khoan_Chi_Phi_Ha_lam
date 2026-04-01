using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Material.Commands;

public record UpdateMaterialCommand(UpdateMaterialDto UpdateModel) : IRequest<bool>;

public class UpdateMaterialCommandHandler(IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<UpdateMaterialCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();
    private readonly IWriteRepository<AssignmentCode> _assigmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    public async Task<bool> Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        if (request.UpdateModel.MaterialType == Domain.Common.Enums.MaterialType.MaterialInContract)
        {
            bool checkAssigmentCodeExisted = await _assigmentCodeRepository.ExistsAsync(x => x.Id == request.UpdateModel.AssigmentCodeId);
            if (!checkAssigmentCodeExisted)
            {
                throw new NotFoundException(CustomResponseMessage.AssignmentCodeAlreadyExists);
            }
        }

        if (request.UpdateModel.UnitOfMeasureId != null)
        {
            bool checkUnitOfMeasureExisted = await _unitOfMeasureRepository.ExistsAsync(x => x.Id == request.UpdateModel.UnitOfMeasureId);
            if (!checkUnitOfMeasureExisted)
            {
                throw new NotFoundException(CustomResponseMessage.UnitOfMeasureNotFound);
            }
        }

        var existedMaterial = await _materialRepository.GetFirstOrDefaultAsync(
            predicate: m => m.Id == request.UpdateModel.Id,
            include: m => m.Include(c => c.Costs).Include(c => c.Code),
            disableTracking: true) ?? throw new NotFoundException(CustomResponseMessage.MaterialNotFound);

        if (await codeService.IsCodeExisted(request.UpdateModel.Code, existedMaterial.CodeId))
        {
            throw new ConflictException(CustomResponseMessage.MaterialCodeAlreadyExists);
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {

            _costRepository.Delete(existedMaterial.Costs.ToList());
            await unitOfWork.SaveChangesAsync();

            existedMaterial.Update(request.UpdateModel.Code, request.UpdateModel.Name, request.UpdateModel.UnitOfMeasureId, request.UpdateModel.AssigmentCodeId, request.UpdateModel.MaterialType);

            var costList = new List<Cost>();
            foreach (var cost in request.UpdateModel.Costs)
            {
                costList.Add(Cost.Create(
                    startMonth: cost.StartMonth,
                    endMonth: cost.EndMonth,
                    costType: cost.CostType,
                    amount: cost.Amount,
                    actualAmount: cost.ActualAmount,
                    costTypeId: existedMaterial.Id));
            }

            if (costList.HasOverlap())
            {
                throw new ConflictException(CustomResponseMessage.CostTimeOverlap);
            }

            existedMaterial.AddMaterialCost(costList);

            _materialRepository.Update(existedMaterial);

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
