using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.WasteSuctionTruckUnitPrice.Commands;

public record ImportWasteSuctionTruckUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportWasteSuctionTruckUnitPriceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportWasteSuctionTruckUnitPriceExcelCommand, bool>
{
    private static readonly string[] ValidEquipmentQualities = { "A", "B", "C" };

    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice> _repository =
        unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<HaulDistance> _haulDistanceRepository = unitOfWork.GetRepository<HaulDistance>();

    private sealed record ValidatedRow(
        Guid HeaderId,
        Guid AssignmentCodeId,
        string EquipmentQuality,
        Guid ProductionProcessId,
        DateOnly StartMonth,
        DateOnly EndMonth,
        Guid? HaulDistanceId,
        decimal FuelUnitPrice,
        decimal MaintenanceUnitPrice);

    public async Task<bool> Handle(ImportWasteSuctionTruckUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        List<string> importErrors = [];

        using Stream stream = request.File.OpenReadStream();
        List<WasteSuctionTruckUnitPriceExcelDto> dtos = excelService.ImportFromExcel<WasteSuctionTruckUnitPriceExcelDto>(stream) ?? [];

        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(include: q => q.Include(a => a.Code), disableTracking: true);
        var assignmentCodeMap = assignmentCodes.Where(a => a.Code != null).ToDictionary(a => a.Code!.Value.Trim(), a => a, StringComparer.OrdinalIgnoreCase);

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTCG,
            include: q => q.Include(p => p.Code),
            disableTracking: true);
        var productionProcessMap = productionProcesses.Where(p => p.Code != null).ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var haulDistances = await _haulDistanceRepository.GetAllAsync(disableTracking: true);
        var haulDistanceMap = haulDistances.ToDictionary(h => h.Value.Trim(), h => h, StringComparer.OrdinalIgnoreCase);

        var validRows = new List<ValidatedRow>();

        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                string? assignmentCode = ExtractCode(item.dto.AssignmentCodeCode);
                if (assignmentCode == null || !assignmentCodeMap.TryGetValue(assignmentCode, out AssignmentCode? assignment))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: nhóm vật tư, tài sản '{item.dto.AssignmentCodeCode}' không tồn tại.");
                    continue;
                }

                string? productionProcessCode = ExtractCode(item.dto.ProductionProcessCode);
                if (productionProcessCode == null || !productionProcessMap.TryGetValue(productionProcessCode, out ProductionProcess? productionProcess))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: công đoạn sản xuất '{item.dto.ProductionProcessCode}' không tồn tại hoặc không thuộc Vận tải cơ giới.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.dto.EquipmentQuality) || !ValidEquipmentQualities.Contains(item.dto.EquipmentQuality.Trim().ToUpper()))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: chất lượng thiết bị phải là A, B hoặc C.");
                    continue;
                }

                Guid? haulDistanceId = null;
                if (!string.IsNullOrWhiteSpace(item.dto.HaulDistanceValue))
                {
                    if (!haulDistanceMap.TryGetValue(item.dto.HaulDistanceValue.Trim(), out HaulDistance? haulDistance))
                    {
                        importErrors.Add($"Dòng {item.rowNumber}: cung độ vận tải '{item.dto.HaulDistanceValue}' không tồn tại.");
                        continue;
                    }
                    haulDistanceId = haulDistance.Id;
                }

                DateOnly startMonth = ParseMonthYear(item.dto.StartMonth);
                DateOnly endMonth = ParseMonthYear(item.dto.EndMonth);

                validRows.Add(new ValidatedRow(
                    item.dto.HeaderId,
                    assignment.Id,
                    item.dto.EquipmentQuality.Trim().ToUpper(),
                    productionProcess.Id,
                    startMonth,
                    endMonth,
                    haulDistanceId,
                    item.dto.FuelUnitPrice,
                    item.dto.MaintenanceUnitPrice));
            }
            catch (Exception ex) when (ex is BadRequestException or ArgumentException)
            {
                importErrors.Add($"Dòng {item.rowNumber}: {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var groups = validRows
            .GroupBy(r => r.HeaderId != Guid.Empty
                ? $"ID:{r.HeaderId}"
                : $"NEW:{r.AssignmentCodeId}|{r.EquipmentQuality}|{r.ProductionProcessId}|{r.StartMonth}|{r.EndMonth}")
            .ToList();

        foreach (var group in groups)
        {
            var distanceIds = group.Select(r => r.HaulDistanceId).ToList();
            if (distanceIds.Count != distanceIds.Distinct().Count())
            {
                importErrors.Add($"Nhóm '{group.First().AssignmentCodeId}': trùng cung độ vận tải trong cùng 1 header.");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbEntities = await _repository.GetAllAsync(include: q => q.Include(w => w.Details), disableTracking: false);

        var deleteList = new List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();
        var updateList = new List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();
        var addList = new List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice>();

        var keptHeaderIds = groups
            .Where(g => g.Key.StartsWith("ID:"))
            .Select(g => g.First().HeaderId)
            .ToList();
        deleteList.AddRange(dbEntities.Where(x => !keptHeaderIds.Contains(x.Id)));

        foreach (var group in groups)
        {
            var first = group.First();
            var details = group.Select(r => new MechanizedTransportUnitPriceDetailInput(r.HaulDistanceId, r.FuelUnitPrice, null, r.MaintenanceUnitPrice));

            if (group.Key.StartsWith("ID:"))
            {
                var entityToUpdate = dbEntities.FirstOrDefault(x => x.Id == first.HeaderId);
                if (entityToUpdate == null)
                {
                    importErrors.Add($"Không tìm thấy bản ghi với Id '{first.HeaderId}' để cập nhật.");
                    continue;
                }

                entityToUpdate.Update(first.AssignmentCodeId, first.EquipmentQuality, first.ProductionProcessId, first.StartMonth, first.EndMonth, details);
                updateList.Add(entityToUpdate);
            }
            else
            {
                var isDuplicated = dbEntities.Any(x =>
                    x.AssignmentCodeId == first.AssignmentCodeId
                    && x.EquipmentQuality == first.EquipmentQuality
                    && x.ProductionProcessId == first.ProductionProcessId
                    && x.StartMonth == first.StartMonth
                    && x.EndMonth == first.EndMonth);

                if (isDuplicated)
                {
                    importErrors.Add($"Đã tồn tại đơn giá cho thiết bị/công đoạn/khoảng thời gian này (thiếu HeaderId để cập nhật).");
                    continue;
                }

                addList.Add(Domain.Entities.Pricing.MechanizedTransportUnitPrice.WasteSuctionTruckUnitPrice.Create(
                    first.AssignmentCodeId, first.EquipmentQuality, first.ProductionProcessId, first.StartMonth, first.EndMonth, details));
            }
        }

        ThrowIfImportErrors(importErrors);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Count > 0)
            {
                _repository.Delete(deleteList);
            }

            if (addList.Count > 0)
            {
                await _repository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Count > 0)
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
        var errors = importErrors.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.Ordinal).ToList();
        if (errors.Count == 0)
        {
            return;
        }
        throw new ExcelImportException(errors);
    }
}