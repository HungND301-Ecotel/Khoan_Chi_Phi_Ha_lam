using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Part;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using PartEntity = Domain.Entities.Index.Part;

namespace Application.Catalog.Index.Part.Commands;

public record ImportPartExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportPartExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportPartExcelCommand, bool>
{
    private readonly IWriteRepository<PartEntity> _partRepository = unitOfWork.GetRepository<PartEntity>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    public async Task<bool> Handle(ImportPartExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<PartExcelDto>(stream);

        if (!(await CheckExistedUnitOfMeasure(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        if (!(await CheckExistedEquipment(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.EquipmentNotFound);
        }

        //Map data to Entity Model

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.UnitOfMeasureName).Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name, p => p.Id);

        var equipment = await _equipmentRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.EquipmentCode).Contains(p.Code!.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var equipmentIdMap = equipment.ToDictionary(p => p.Code!.Value, p => p.Id);


        var excelDtos = dtos.Select(d =>
        {
            if (unitOfMeasureIdMap.TryGetValue(d.UnitOfMeasureName, out var unitOfMeasureId) && equipmentIdMap.TryGetValue(d.EquipmentCode, out var equipmentId))
            {
                var partEntity = PartEntity.Create(d.Id, d.Code, d.Name, unitOfMeasureId, equipmentId);
                var costList = costService.ParseExcelCostString(d.Cost, Domain.Common.Enums.CostType.Part, Guid.Empty);
                partEntity.AddCost(costList);
                return partEntity;
            }
            else
            {
                return null;
            }
        }).Where(d => d != null).ToList();

        var dbParts = await _partRepository.GetAllAsync(
            include: p => p.Include(p => p.Code).Include(p => p.Costs),
            disableTracking: true);

        var deleteList = new List<PartEntity>();
        var deleteCost = new List<Cost>();
        var updateList = new List<PartEntity>();
        var addList = new List<PartEntity>();

        //CheckDelete
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
                    if (await codeService.IsPartCodeExisted(dto.Code.Value, entityToUpdate.CodeId, dto.EquipmentId))
                    {
                        throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code.Value, dto.Name, dto.UnitOfMeasureId, dto.EquipmentId);

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
                // 3. Thêm mới nếu không tìm thấy ID
                if (await codeService.IsPartCodeExisted(dto.Code.Value, dto.EquipmentId))
                {
                    throw new ConflictException(CustomResponseMessage.PartCodeAlreadyExists);
                }

                var newPart = PartEntity.Create(dto.Code.Value, dto.Name, dto.UnitOfMeasureId, dto.EquipmentId);
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

    private async Task<bool> CheckExistedUnitOfMeasure(List<PartExcelDto> dtoList)
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

    private async Task<bool> CheckExistedEquipment(List<PartExcelDto> dtoList)
    {
        var dbProcessCodes = (await _equipmentRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var excelProcessCodes = dtoList
            .Select(d => d.EquipmentCode?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct();

        return excelProcessCodes.All(code => dbProcessCodes.Contains(code));
    }
}