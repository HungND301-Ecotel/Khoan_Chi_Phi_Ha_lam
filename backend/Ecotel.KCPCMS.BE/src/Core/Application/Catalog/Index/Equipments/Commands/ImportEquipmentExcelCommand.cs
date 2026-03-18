using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.Equipments.Commands;

public record ImportEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(ImportEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<EquipmentExcelDto>(stream);

        if (!(await CheckExistedUnitOfMeasure(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.UnitOfMeasureNotFound);
        }

        //Map data to Entity Model

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.UnitOfMeasureName).Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (unitOfMeasureIdMap.TryGetValue(d.UnitOfMeasureName, out var unitOfMeasureId))
            {
                var partEntity = Equipment.Create(d.Id, d.Code, d.Name, unitOfMeasureId);
                var costList = costService.ParseExcelCostString(d.Cost, Domain.Common.Enums.CostType.Electricity, Guid.Empty);
                partEntity.AddCost(costList);
                return partEntity;
            }
            else
            {
                return null;
            }
        }).Where(d => d != null).ToList();

        var dbEquipments = await _equipmentRepository.GetAllAsync(
            include: p => p.Include(p => p.Code).Include(p => p.Costs),
            disableTracking: true);

        var deleteList = new List<Equipment>();
        var deleteCost = new List<Cost>();
        var updateList = new List<Equipment>();
        var addList = new List<Equipment>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEquipments.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        var dbPartDict = dbEquipments.ToDictionary(p => p.Id);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbPartDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                bool isInfoChanged = entityToUpdate.CheckChange(dto);
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), dto.Costs.ToList());

                if (isInfoChanged || isCostChanged)
                {
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code.Value, dto.Name, dto.UnitOfMeasureId);

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
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                }

                var newPart = Equipment.Create(dto.Code.Value, dto.Name, dto.UnitOfMeasureId);
                newPart.AddCost(dto.Costs.ToList());
                addList.Add(newPart);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _equipmentRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _equipmentRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                if (deleteCost.Any())
                {
                    _costRepository.Delete(deleteCost);
                }
                _equipmentRepository.Update(updateList);
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

    private async Task<bool> CheckExistedUnitOfMeasure(List<EquipmentExcelDto> dtoList)
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