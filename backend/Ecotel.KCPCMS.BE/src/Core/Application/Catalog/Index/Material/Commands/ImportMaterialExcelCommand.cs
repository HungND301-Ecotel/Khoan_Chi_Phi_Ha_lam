using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using MaterialEntity = Domain.Entities.Index.Material;

namespace Application.Catalog.Index.Material.Commands;

public record ImportMaterialExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportMaterialExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICostService costService, ICodeService codeService) : IRequestHandler<ImportMaterialExcelCommand, bool>
{
    private readonly IWriteRepository<MaterialEntity> _materialRepository = unitOfWork.GetRepository<MaterialEntity>();
    private readonly IWriteRepository<Cost> _costRepository = unitOfWork.GetRepository<Cost>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    public async Task<bool> Handle(ImportMaterialExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<MaterialExcelDto>(stream);
        if (dtos == null || !dtos.Any())
        {
            return true;
        }

        // 1. Kiểm tra AssignmentCode hợp lệ cho các dòng không phải OutContract
        var dtoRequiringAssignmentCode = dtos.Where(d =>
            !string.Equals(d.MaterialType?.Trim(), MaterialType.MaterialOutContract.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(d.MaterialType?.Trim(), ((int)MaterialType.MaterialOutContract).ToString())
        ).ToList();

        if (!(await CheckExistedAssignmentCode(dtoRequiringAssignmentCode)))
        {
            throw new BadRequestException(CustomResponseMessage.AssignmentCodeNotFound);
        }

        // 2. Tải Map dữ liệu tham chiếu (UoM và AssignmentCode)
        var uomNames = dtos.Select(d => d.UnitOfMeasureName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        var unitOfMeasureIdMap = (await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => uomNames.Contains(p.Name),
            disableTracking: true)).ToDictionary(p => p.Name, p => p.Id);

        var acCodes = dtos.Select(d => d.AssignmentCode?.Trim()).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
        var assignmentCodeIdMap = (await _assignmentCodeRepository.GetAllAsync(
            predicate: p => acCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true)).ToDictionary(p => p.Code.Value, p => p.Id);

        // 3. Xử lý logic nghiệp vụ & Mapping
        var dbEquipments = await _materialRepository.GetAllAsync(
            include: p => p.Include(p => p.Code).Include(p => p.Costs),
            disableTracking: true);
        var dbMaterialDict = dbEquipments.ToDictionary(p => p.Id);

        var deleteList = new List<MaterialEntity>();
        var deleteCost = new List<Cost>();
        var updateList = new List<MaterialEntity>();
        var addList = new List<MaterialEntity>();

        // Danh sách ID từ Excel để xác định các bản ghi cần xóa
        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToHashSet();
        deleteList.AddRange(dbEquipments.Where(x => !excelIds.Contains(x.Id)));

        foreach (var dto in dtos)
        {
            var materialType = dto.MaterialType?.Trim().ParseFromDisplayName<MaterialType>();

            if (materialType == null)
            {
                if (!Enum.TryParse<MaterialType>(dto.MaterialType?.Trim(), out var parsedType))
                {
                    throw new ConflictException(CustomResponseMessage.InvalidMaterialType);
                }
                materialType = parsedType;
            }

            bool isOutContract = materialType.Value == MaterialType.MaterialOutContract;

            // Lấy ID tham chiếu
            unitOfMeasureIdMap.TryGetValue(dto.UnitOfMeasureName?.Trim() ?? "", out var uomId);
            Guid? unitOfMeasureId = uomId != Guid.Empty ? uomId : null;

            Guid? assignmentCodeId = null;
            if (!isOutContract)
            {
                if (!assignmentCodeIdMap.TryGetValue(dto.AssignmentCode?.Trim() ?? "", out var acId))
                {
                    throw new ConflictException(CustomResponseMessage.AssignmentCodeNotFound);
                }

                assignmentCodeId = acId;
            }

            // Xử lý Cost
            var incomingCosts = costService.ParseExcelCostString(dto.Cost, CostType.Material, Guid.Empty);

            if (dto.Id != Guid.Empty && dbMaterialDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                // Logic UPDATE
                bool isInfoChanged = entityToUpdate.CheckChange(MaterialEntity.Create(dto.Id, dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, dto.UsageTime, materialType.Value));
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), incomingCosts);

                if (isInfoChanged || isCostChanged)
                {
                    if (await codeService.IsCodeExisted(dto.Code, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, dto.UsageTime, materialType.Value);

                    if (isCostChanged)
                    {
                        deleteCost.AddRange(entityToUpdate.Costs);
                        entityToUpdate.ClearCost();
                        entityToUpdate.AddMaterialCost(incomingCosts);
                    }
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                // Logic ADD NEW
                if (await codeService.IsCodeExisted(dto.Code))
                {
                    throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                }

                var newMaterial = MaterialEntity.Create(dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, dto.UsageTime, materialType.Value);
                newMaterial.AddMaterialCost(incomingCosts);
                addList.Add(newMaterial);
            }
        }

        // 4. Thực thi Database Transaction
        await unitOfWork.BeginTransactionAsync();
        try
        {
            if (deleteList.Any())
            {
                _materialRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _materialRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                if (deleteCost.Any())
                {
                    _costRepository.Delete(deleteCost);
                }

                _materialRepository.Update(updateList);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
    private async Task<bool> CheckExistedUnitOfMeasure(List<MaterialExcelDto> dtoList)
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

    private async Task<bool> CheckExistedAssignmentCode(List<MaterialExcelDto> dtoList)
    {
        var dbAssignmentCode = (await _assignmentCodeRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code?.Value?.Trim())
            .Where(code => code != null)
            .ToHashSet();

        var excelAssignmentCode = dtoList
            .Select(d => d.AssignmentCode?.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct();

        return excelAssignmentCode.All(code => dbAssignmentCode.Contains(code));
    }
}