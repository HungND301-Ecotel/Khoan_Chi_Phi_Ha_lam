using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportUnitPrice;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using ProductionProcessEntity = Domain.Entities.Index.ProductionProcess;
using TransportUnitPriceEntity = Domain.Entities.Pricing.TransportUnitPrice;

namespace Application.Catalog.Pricing.TransportUnitPrice.Commands;

public record ImportTransportUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTransportUnitPriceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportTransportUnitPriceExcelCommand, bool>
{
    private readonly IWriteRepository<TransportUnitPriceEntity> _repository = unitOfWork.GetRepository<TransportUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcessEntity> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcessEntity>();
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<AssignmentCode> _equipmentRepository = unitOfWork.GetRepository<AssignmentCode>();

    public async Task<bool> Handle(ImportTransportUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        List<string> importErrors = [];

        using Stream stream = request.File.OpenReadStream();
        List<TransportUnitPriceExcelDto> dtos = excelService.ImportFromExcel<TransportUnitPriceExcelDto>(stream) ?? [];

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTL,
            include: query => query.Include(p => p.Code),
            disableTracking: true);
        Dictionary<string, ProductionProcessEntity> productionProcessMap = productionProcesses
            .Where(p => p.Code != null)
            .ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var transportRoutes = await _transportRouteRepository.GetAllAsync(
            include: query => query.Include(r => r.Code),
            disableTracking: true);
        Dictionary<string, TransportRoute> transportRouteMap = transportRoutes
            .Where(r => r.Code != null)
            .ToDictionary(r => r.Code!.Value.Trim(), r => r, StringComparer.OrdinalIgnoreCase);

        var departments = await _departmentRepository.GetAllAsync(
            include: query => query.Include(d => d.Code),
            disableTracking: true);
        Dictionary<string, Department> departmentMap = departments
            .Where(d => d.Code != null)
            .ToDictionary(d => d.Code!.Value.Trim(), d => d, StringComparer.OrdinalIgnoreCase);

        var equipments = await _equipmentRepository.GetAllAsync(
            include: query => query.Include(eq => eq.Code),
            disableTracking: true);
        Dictionary<string, AssignmentCode> equipmentMap = equipments
            .Where(eq => eq.Code != null)
            .ToDictionary(eq => eq.Code!.Value.Trim(), eq => eq, StringComparer.OrdinalIgnoreCase);

        List<TransportUnitPriceEntity> excelEntities = [];
        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                string? productionProcessCode = ExtractCode(item.dto.ProductionProcessCode);
                if (productionProcessCode == null ||
                    !productionProcessMap.TryGetValue(productionProcessCode, out ProductionProcessEntity? productionProcess))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: công đoạn sản xuất '{item.dto.ProductionProcessCode}' không tồn tại hoặc không thuộc Vận tải lò.");
                    continue;
                }

                Guid? transportRouteId = null;
                string? transportRouteCode = ExtractCode(item.dto.TransportRouteCode);
                if (transportRouteCode != null)
                {
                    if (!transportRouteMap.TryGetValue(transportRouteCode, out TransportRoute? transportRoute))
                    {
                        importErrors.Add($"Dòng {item.rowNumber}: tuyến vận tải '{item.dto.TransportRouteCode}' không tồn tại.");
                        continue;
                    }
                    transportRouteId = transportRoute.Id;
                }

                Guid? departmentId = null;
                string? departmentCode = ExtractCode(item.dto.DepartmentCode);
                if (departmentCode != null)
                {
                    if (!departmentMap.TryGetValue(departmentCode, out Department? department))
                    {
                        importErrors.Add($"Dòng {item.rowNumber}: đơn vị '{item.dto.DepartmentCode}' không tồn tại.");
                        continue;
                    }
                    departmentId = department.Id;
                }

                Guid? equipmentId = null;
                string? equipmentCode = ExtractCode(item.dto.EquipmentCode);
                if (equipmentCode != null)
                {
                    if (!equipmentMap.TryGetValue(equipmentCode, out AssignmentCode? equipment))
                    {
                        importErrors.Add($"Dòng {item.rowNumber}: nhóm vật tư, tài sản '{item.dto.EquipmentCode}' không tồn tại.");
                        continue;
                    }
                    equipmentId = equipment.Id;
                }

                if (transportRouteId.HasValue && equipmentId.HasValue)
                {
                    importErrors.Add($"Dòng {item.rowNumber}: không được chọn đồng thời Tuyến vận tải và Nhóm vật tư, tài sản.");
                    continue;
                }

                DateOnly startMonth = ParseMonthYear(item.dto.StartMonth);
                DateOnly endMonth = ParseMonthYear(item.dto.EndMonth);

                TransportUnitPriceEntity entity = TransportUnitPriceEntity.Create(
                    productionProcess.Id,
                    transportRouteId,
                    departmentId,
                    equipmentId,
                    string.IsNullOrWhiteSpace(item.dto.EquipmentQuality) ? null : item.dto.EquipmentQuality.Trim(),
                    item.dto.MaterialFuelUnitPrice,
                    item.dto.PowerUnitPrice,
                    item.dto.MaintenanceUnitPrice,
                    item.dto.Quantity,
                    item.dto.IsLowVolumeCase,
                    startMonth,
                    endMonth);

                if (item.dto.Id != Guid.Empty)
                {
                    entity.GetType().GetProperty("Id")?.SetValue(entity, item.dto.Id);
                }

                excelEntities.Add(entity);
            }
            catch (Exception ex) when (ex is BadRequestException or ArgumentException)
            {
                importErrors.Add($"Dòng {item.rowNumber}: {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbEntities = await _repository.GetAllAsync(disableTracking: false);

        List<TransportUnitPriceEntity> deleteList = [];
        List<TransportUnitPriceEntity> updateList = [];
        List<TransportUnitPriceEntity> addList = [];

        List<Guid> excelIds = excelEntities.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        deleteList.AddRange(dbEntities.Where(x => !excelIds.Contains(x.Id)));

        foreach (TransportUnitPriceEntity excelEntity in excelEntities)
        {
            if (excelEntity.Id != Guid.Empty && dbEntities.Any(x => x.Id == excelEntity.Id))
            {
                TransportUnitPriceEntity entityToUpdate = dbEntities.First(x => x.Id == excelEntity.Id);
                entityToUpdate.Update(
                    excelEntity.ProductionProcessId,
                    excelEntity.TransportRouteId,
                    excelEntity.DepartmentId,
                    excelEntity.EquipmentId,
                    excelEntity.EquipmentQuality,
                    excelEntity.MaterialFuelUnitPrice,
                    excelEntity.PowerUnitPrice,
                    excelEntity.MaintenanceUnitPrice,
                    excelEntity.Quantity,
                    excelEntity.IsLowVolumeCase,
                    excelEntity.StartMonth,
                    excelEntity.EndMonth);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(excelEntity);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _repository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _repository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _repository.Update(updateList);
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

    private static string? ExtractCode(string? combinedValue)
    {
        if (string.IsNullOrWhiteSpace(combinedValue))
        {
            return null;
        }

        int separatorIndex = combinedValue.IndexOf(" - ", StringComparison.Ordinal);
        string code = separatorIndex >= 0 ? combinedValue[..separatorIndex] : combinedValue;
        return code.Trim();
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            return DateOnly.MinValue;
        }

        if (DateOnly.TryParseExact(monthYear, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateOnly result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(monthYear, "M/yyyy", null, System.Globalization.DateTimeStyles.None, out result))
        {
            return result;
        }

        if (DateTime.TryParse(monthYear, out DateTime dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        throw new BadRequestException($"Không thể parse tháng năm: {monthYear}. Định dạng cần là MM/yyyy hoặc M/yyyy");
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        List<string> errors = importErrors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ExcelImportException(errors);
    }
}