using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Material;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MaterialEntity = Domain.Entities.Index.Material;

namespace Application.Catalog.Index.Material.Commands;

public record ImportMaterialExcelCommand(IFormFile File, MaterialType MaterialType) : IRequest<bool>;

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

        var isOutContract = request.MaterialType == MaterialType.MaterialOutContract;

        // 1. Tải map dữ liệu tham chiếu (UoM và AssignmentCode)
        var uomNames = dtos.Select(d => d.UnitOfMeasureName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        var unitOfMeasureIdMap = (await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => uomNames.Contains(p.Name),
            disableTracking: true)).ToDictionary(p => p.Name, p => p.Id);

        var acCodes = dtos
            .Select(d => d.AssignmentCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();
        var assignmentCodeIdMap = (await _assignmentCodeRepository.GetAllAsync(
            predicate: p => acCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true)).ToDictionary(p => p.Code.Value, p => p.Id);

        // 2. Chỉ xử lý dữ liệu trong phạm vi MaterialType của request
        var dbMaterials = await _materialRepository.GetAllAsync(
            predicate: p => p.MaterialType == request.MaterialType,
            include: p => p.Include(p => p.Code).Include(p => p.Costs),
            disableTracking: true);
        var dbMaterialDict = dbMaterials.ToDictionary(p => p.Id);

        var deleteList = new List<MaterialEntity>();
        var deleteCost = new List<Cost>();
        var updateList = new List<MaterialEntity>();
        var addList = new List<MaterialEntity>();

        // Danh sách ID từ Excel để xác định các bản ghi cần xóa trong đúng MaterialType
        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToHashSet();
        deleteList.AddRange(dbMaterials.Where(x => !excelIds.Contains(x.Id)));

        // 3. Xử lý mapping + validate theo từng dòng
        for (int i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var rowNumber = i + 2; // Excel row (header là dòng 1)

            if (dto.Id != Guid.Empty && !dbMaterialDict.ContainsKey(dto.Id))
            {
                throw new BadRequestException($"Dòng {rowNumber}: Không tìm thấy vật tư/tài sản với Id '{dto.Id}' trong nhóm materialType '{request.MaterialType}'.");
            }

            // Lấy ID tham chiếu
            var unitOfMeasureName = dto.UnitOfMeasureName?.Trim() ?? string.Empty;
            unitOfMeasureIdMap.TryGetValue(unitOfMeasureName, out var uomId);
            Guid? unitOfMeasureId = uomId != Guid.Empty ? uomId : null;
            if (!string.IsNullOrEmpty(unitOfMeasureName) && unitOfMeasureId == null)
            {
                throw new BadRequestException($"Dòng {rowNumber}: Đơn vị tính '{unitOfMeasureName}' không tồn tại.");
            }

            Guid? assignmentCodeId = null;
            if (!isOutContract)
            {
                var assignmentCode = dto.AssignmentCode?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(assignmentCode))
                {
                    throw new BadRequestException($"Dòng {rowNumber}: Thiếu mã giao khoán cho materialType '{request.MaterialType}'.");
                }
                if (!assignmentCodeIdMap.TryGetValue(assignmentCode, out var acId))
                {
                    throw new BadRequestException($"Dòng {rowNumber}: Mã giao khoán '{assignmentCode}' không tồn tại.");
                }
                assignmentCodeId = acId;
            }

            // Xử lý Cost
            List<Cost> incomingCosts;
            try
            {
                incomingCosts = costService.ParseExcelCostString(dto.Cost, CostType.Material, Guid.Empty);
            }
            catch (Exception ex)
            {
                throw new BadRequestException($"Dòng {rowNumber}: Đơn giá '{dto.Cost}' không hợp lệ. Chi tiết: {ex.Message}");
            }

            if (dto.Id != Guid.Empty && dbMaterialDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                // Logic UPDATE
                bool isInfoChanged = entityToUpdate.CheckChange(MaterialEntity.Create(dto.Id, dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, request.MaterialType));
                bool isCostChanged = costService.AreCostsChanged(entityToUpdate.Costs.ToList(), incomingCosts);

                if (isInfoChanged || isCostChanged)
                {
                    if (await codeService.IsCodeExisted(dto.Code, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException($"Dòng {rowNumber}: Mã vật tư/tài sản '{dto.Code}' đã tồn tại.");
                    }

                    entityToUpdate.Update(dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, request.MaterialType);

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
                    throw new ConflictException($"Dòng {rowNumber}: Mã vật tư/tài sản '{dto.Code}' đã tồn tại.");
                }

                var newMaterial = MaterialEntity.Create(dto.Code, dto.Name, unitOfMeasureId, assignmentCodeId, request.MaterialType);
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
}
