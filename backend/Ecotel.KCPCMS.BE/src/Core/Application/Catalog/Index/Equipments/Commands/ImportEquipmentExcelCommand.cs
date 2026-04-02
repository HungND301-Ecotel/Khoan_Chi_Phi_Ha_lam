using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Equipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Equipments.Commands;

public record ImportEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(ImportEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<EquipmentExcelDto>(stream);

        //Map data to Entity Model

        var dbUnitOfMeasureNames = (await _unitOfMeasureRepository.GetAllAsync(disableTracking: true))
            .Select(p => p.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dtos.Count; i++)
        {
            var unitName = dtos[i].UnitOfMeasureName?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(unitName) || !dbUnitOfMeasureNames.Contains(unitName))
            {
                throw new BadRequestException($"Giá trị đơn vị tính '{dtos[i].UnitOfMeasureName}' không tồn tại ở dòng {i + 2}.");
            }
        }

        var unitNames = dtos
            .Select(d => d.UnitOfMeasureName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var excelDtos = new List<Equipment>();
        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var rowNumber = i + 2;
            var unitName = dto.UnitOfMeasureName?.Trim() ?? string.Empty;

            if (!unitOfMeasureIdMap.TryGetValue(unitName, out var unitOfMeasureId))
            {
                throw new BadRequestException($"Giá trị đơn vị tính '{dto.UnitOfMeasureName}' không tồn tại ở dòng {rowNumber}.");
            }

            try
            {
                var equipmentEntity = Equipment.Create(dto.Id, dto.Code, dto.Name, unitOfMeasureId);
                var costList = costService.ParseExcelCostString(dto.Cost, Domain.Common.Enums.CostType.Electricity, Guid.Empty);
                equipmentEntity.AddCost(costList);
                excelDtos.Add(equipmentEntity);
            }
            catch (Exception ex)
            {
                throw new BadRequestException($"Giá trị đơn giá '{dto.Cost}' không hợp lệ ở dòng {rowNumber}. Chi tiết: {ex.Message}");
            }
        }

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
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        var dbPartDict = dbEquipments.ToDictionary(p => p.Id);

        for (var i = 0; i < excelDtos.Count; i++)
        {
            var dto = excelDtos[i];
            var rowNumber = i + 2;
            if (dto.Id != Guid.Empty && dbPartDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                bool isInfoChanged = entityToUpdate.CheckChange(dto);
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), dto.Costs.ToList());

                if (isInfoChanged || isCostChanged)
                {
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException($"Giá trị mã '{dto.Code.Value}' đã tồn tại ở dòng {rowNumber}.");
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
                    throw new ConflictException($"Giá trị mã '{dto.Code.Value}' đã tồn tại ở dòng {rowNumber}.");
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

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
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

}
