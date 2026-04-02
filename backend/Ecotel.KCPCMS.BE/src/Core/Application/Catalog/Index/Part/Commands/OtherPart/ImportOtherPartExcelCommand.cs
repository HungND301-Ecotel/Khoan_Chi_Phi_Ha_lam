using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Commands.Part;

public record ImportOtherPartExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportOtherPartExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportOtherPartExcelCommand, bool>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();

    public async Task<bool> Handle(ImportOtherPartExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<OtherPartExcelDto>(stream);

        if (!(await CheckExistedUnitOfMeasure(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.UnitOfMeasureName).Contains(p.Name),
            disableTracking: false);

        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (!unitOfMeasureIdMap.TryGetValue(d.UnitOfMeasureName, out var unitOfMeasureId))
            {
                return null;
            }

            var partEntity = PartEntity.Create(d.Id, d.Code, d.Name, unitOfMeasureId, d.ReplacementTimeStandard, PartType.OtherPart);
            var costList = costService.ParseExcelCostString(d.Cost, Domain.Common.Enums.CostType.Part, Guid.Empty);
            partEntity.AddCost(costList);
            return partEntity;
        }).Where(d => d != null).Cast<PartEntity>().ToList();

        var dbParts = await _partRepository.GetAllAsync(
            predicate: p => p.Type == PartType.OtherPart,
            include: p => p.Include(p => p.Code).Include(p => p.Costs).Include(p => p.EquipmentParts),
            disableTracking: false);

        var deleteList = new List<PartEntity>();
        var deleteCost = new List<Cost>();
        var updateList = new List<PartEntity>();
        var addList = new List<PartEntity>();

        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbParts.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        var dbPartDict = dbParts.ToDictionary(p => p.Id);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbPartDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                bool isInfoChanged = entityToUpdate.CheckChange(dto);
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), dto.Costs.ToList());

                if (isInfoChanged || isCostChanged)
                {
                    if (await codeService.IsPartCodeExisted(dto.Code!.Value, entityToUpdate.CodeId))
                    {
                        throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
                    }

                    entityToUpdate.Update(
                        dto.Code!.Value,
                        dto.Name,
                        dto.UnitOfMeasureId,
                        dto.ReplacementTimeStandard,
                        PartType.OtherPart);

                    if (isCostChanged)
                    {
                        deleteCost.AddRange(entityToUpdate.Costs.ToList());

                        entityToUpdate.ClearCost();
                        entityToUpdate.AddCost(dto.Costs.ToList());
                    }

                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsPartCodeExisted(dto.Code!.Value))
                {
                    throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
                }

                var newPart = PartEntity.Create(
                    dto.Code!.Value,
                    dto.Name,
                    dto.UnitOfMeasureId,
                    dto.ReplacementTimeStandard,
                    PartType.OtherPart);
                newPart.AddCost(dto.Costs.ToList());
                addList.Add(newPart);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _partRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _partRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                if (deleteCost.Any())
                {
                    _costRepository.Delete(deleteCost);
                }
                _partRepository.Update(updateList);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private async Task<bool> CheckExistedUnitOfMeasure(List<OtherPartExcelDto> dtoList)
    {
        var dbProcessNames = (await _unitOfMeasureRepository.GetAllAsync(
                disableTracking: true))
            .Select(p => p.Name.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var excelProcessNames = dtoList
            .Select(d => d.UnitOfMeasureName?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct();

        return excelProcessNames.All(name => dbProcessNames.Contains(name));
    }
}
