using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record ImportTrimmingMaintainUnitPriceEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTrimmingMaintainUnitPriceEquipmentExcelCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<ImportTrimmingMaintainUnitPriceEquipmentExcelCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "MaintainUnitPriceEquipment";
    private readonly IWriteRepository<MaintainUnitPrice> _repository = unitOfWork.GetRepository<MaintainUnitPrice>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<bool> Handle(ImportTrimmingMaintainUnitPriceEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        var importErrors = new List<string>();

        using var stream = request.File.OpenReadStream();
        var maintainUnitPrices = ParseTransposedExcel(stream, importErrors);

        await CollectReferenceErrors(maintainUnitPrices, importErrors);
        ThrowIfImportErrors(importErrors);

        // Map data
        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e
                .Include(e => e.Code),
            disableTracking: true);

        var equipmentIdMap = equipments
            .Where(e => e.Code != null)
            .GroupBy(e => e.Code!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var excelItems = new List<TrimmingMaintainUnitPriceImportItem>();
        foreach (var dto in maintainUnitPrices)
        {
            try
            {
                equipmentIdMap.TryGetValue(dto.EquipmentCode, out var equipmentId);
                var startMonth = ParseMonthYear(dto.StartMonth);
                var endMonth = ParseMonthYear(dto.EndMonth);

                var maintainUnitPriceEquipments = dto.PartData
                    .Where(pd => pd.Value.HasValue)
                    .Select(pd => Domain.Entities.Pricing.MaintainUnitPriceEquipment.Create(
                        null,
                        pd.Key,
                        pd.Value!.Value.Quantity,
                        pd.Value.Value.AverageMonthlyTunnelProduction,
                        pd.Value.Value.ReplacementTimeStandard))
                    .ToList();

                excelItems.Add(new TrimmingMaintainUnitPriceImportItem
                {
                    EquipmentCode = dto.EquipmentCode,
                    EquipmentId = equipmentId,
                    StartMonth = startMonth,
                    EndMonth = endMonth,
                    Parts = maintainUnitPriceEquipments,
                    IsDeleteRequest = dto.IsDeleteRequest
                });
            }
            catch (Exception ex) when (ex is BadRequestException or ArgumentException)
            {
                importErrors.Add($"Thiết bị '{dto.EquipmentCode}': {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbEntities = await _repository.GetAllAsync(
            predicate: m => m.Type == MaintainUnitPriceType.Trimming,
            include: m => m.Include(m => m.MaintainUnitPriceEquipments),
            disableTracking: false);

        var dbLookup = dbEntities
            .GroupBy(x => new TrimmingMaintainUnitPriceLookupKey(x.EquipmentId, x.StartMonth, x.EndMonth))
            .ToDictionary(g => g.Key, g => g.First());

        var deleteList = new List<MaintainUnitPrice>();
        var updateList = new List<MaintainUnitPrice>();
        var addList = new List<MaintainUnitPrice>();
        var processedKeys = new HashSet<TrimmingMaintainUnitPriceLookupKey>();

        foreach (var excelItem in excelItems)
        {
            var key = new TrimmingMaintainUnitPriceLookupKey(excelItem.EquipmentId, excelItem.StartMonth, excelItem.EndMonth);
            if (!processedKeys.Add(key))
            {
                importErrors.Add($"Thiết bị '{excelItem.EquipmentCode}' bị trùng khoảng thời gian {excelItem.StartMonth:MM/yyyy} - {excelItem.EndMonth:MM/yyyy}.");
                continue;
            }

            if (excelItem.IsDeleteRequest)
            {
                if (dbLookup.TryGetValue(key, out var entityToDelete))
                {
                    deleteList.Add(entityToDelete);
                }

                continue;
            }

            if (dbLookup.TryGetValue(key, out var entityToUpdate))
            {
                entityToUpdate.Update(
                    excelItem.EquipmentId,
                    excelItem.StartMonth,
                    excelItem.EndMonth,
                    excelItem.Parts,
                    entityToUpdate.OtherMaterialValue,
                    entityToUpdate.Type);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(MaintainUnitPrice.Create(
                    excelItem.EquipmentId,
                    excelItem.StartMonth,
                    excelItem.EndMonth,
                    excelItem.Parts,
                    null,
                    MaintainUnitPriceType.Trimming));
            }
        }

        ThrowIfImportErrors(importErrors);

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
            cacheService.InvalidateGroup(CacheSignalKey);
            cacheService.InvalidateGroup(ModuleCacheSignalKey);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private List<TrimmingMaintainUnitPriceImportDto> ParseTransposedExcel(Stream stream, ICollection<string> importErrors)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var result = new List<TrimmingMaintainUnitPriceImportDto>();

        // Find number of MaintainUnitPrice groups (each takes 3 columns, starting from column E)
        int colCount = 5;
        while (!worksheet.Cell(1, colCount).IsEmpty())
        {
            colCount += 3;
        }
        colCount -= 3; // Adjust to last filled column group

        // Read each MaintainUnitPrice (every 3 columns from E onwards)
        for (int col = 5; col <= colCount; col += 3)
        {
            var startMonth = worksheet.Cell(1, col).GetString(); // Row 1: StartMonth
            var endMonth = worksheet.Cell(2, col).GetString();   // Row 2: EndMonth
            if (string.IsNullOrWhiteSpace(endMonth))
            {
                endMonth = startMonth; // fallback
            }

            var stateMap = new Dictionary<string, TrimmingEquipmentImportState>(StringComparer.OrdinalIgnoreCase);

            // Read part data (from row 4 onwards, row 3 is header)
            int row = 4; // 👈 đổi từ 3 thành 4
            var currentEquipmentCode = string.Empty;
            while (!worksheet.Cell(row, 3).IsEmpty()) // Check column C (Part Code) for end of data
            {
                var equipmentCodeCellValue = worksheet.Cell(row, 2).GetString().Trim(); // Column B: Equipment Code
                if (!string.IsNullOrWhiteSpace(equipmentCodeCellValue))
                {
                    currentEquipmentCode = equipmentCodeCellValue;
                }

                if (string.IsNullOrWhiteSpace(currentEquipmentCode))
                {
                    row++;
                    continue;
                }

                if (!stateMap.TryGetValue(currentEquipmentCode, out var state))
                {
                    state = new TrimmingEquipmentImportState
                    {
                        Dto = new TrimmingMaintainUnitPriceImportDto
                        {
                            EquipmentCode = currentEquipmentCode,
                            StartMonth = startMonth,
                            EndMonth = endMonth, // 👈 gán đúng endMonth
                            PartData = new Dictionary<Guid, TrimmingPartEquipmentData?>()
                        }
                    };
                    stateMap[currentEquipmentCode] = state;
                }

                var partIdString = worksheet.Cell(row, 1).GetString(); // Column A: Part Id (hidden)
                var replacementTimeStandardValue = worksheet.Cell(row, col).GetString();
                var quantityValue = worksheet.Cell(row, col + 1).GetString();
                var productionValue = worksheet.Cell(row, col + 2).GetString();

                var hasAnyValue = !string.IsNullOrWhiteSpace(replacementTimeStandardValue)
                    || !string.IsNullOrWhiteSpace(quantityValue)
                    || !string.IsNullOrWhiteSpace(productionValue);

                var hasAllValues = !string.IsNullOrWhiteSpace(replacementTimeStandardValue)
                    && !string.IsNullOrWhiteSpace(quantityValue)
                    && !string.IsNullOrWhiteSpace(productionValue);

                if (hasAnyValue && !hasAllValues)
                {
                    importErrors.Add($"Thiếu dữ liệu định mức tại dòng {row}, cụm cột bắt đầu {col}. Vui lòng nhập đủ 3 cột: Định mức thời gian thay thế, Số lượng vật tư 1 lần thay thế, Sản lượng than bình quân tháng.");
                    row++;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(partIdString) &&
                    Guid.TryParse(partIdString, out var partId))
                {
                    state.TotalParts++;

                    if (!hasAllValues)
                    {
                        row++;
                        continue;
                    }

                    if (decimal.TryParse(replacementTimeStandardValue, out var replacementTimeStandard) &&
                        double.TryParse(quantityValue, out var quantity) &&
                        decimal.TryParse(productionValue, out var production))
                    {
                        state.FilledParts++;
                        state.Dto.PartData[partId] = new TrimmingPartEquipmentData
                        {
                            Quantity = quantity,
                            AverageMonthlyTunnelProduction = production,
                            ReplacementTimeStandard = replacementTimeStandard
                        };
                    }
                    else
                    {
                        importErrors.Add($"Dữ liệu số không hợp lệ tại dòng {row}, cụm cột bắt đầu {col}.");
                    }
                }

                row++;
            }

            foreach (var state in stateMap.Values)
            {
                if (state.TotalParts == 0)
                {
                    continue;
                }

                if (state.FilledParts > 0 && state.FilledParts < state.TotalParts)
                {
                    importErrors.Add($"Thiết bị {state.Dto.EquipmentCode} chưa nhập đủ 3 thông số cho tất cả phụ tùng.");
                    continue;
                }

                state.Dto.IsDeleteRequest = state.FilledParts == 0;
                result.Add(state.Dto);
            }
        }

        return result;
    }

    private async Task CollectReferenceErrors(List<TrimmingMaintainUnitPriceImportDto> dtoList, ICollection<string> importErrors)
    {
        var dbEquipmentCodes = (await _equipmentRepository.GetAllAsync(
                include: e => e
                    .Include(e => e.Code),
                disableTracking: true))
            .Where(e => e.Code != null)
            .Select(e => e.Code!.Value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var dto in dtoList)
        {
            var equipmentCode = dto.EquipmentCode?.Trim();
            if (!string.IsNullOrWhiteSpace(equipmentCode) && !dbEquipmentCodes.Contains(equipmentCode))
            {
                importErrors.Add($"Thiết bị '{equipmentCode}' không tồn tại.");
            }
        }
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            return DateOnly.MinValue;
        }

        if (DateOnly.TryParseExact(monthYear, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(monthYear, "M/yyyy", null, System.Globalization.DateTimeStyles.None, out result))
        {
            return result;
        }

        if (DateTime.TryParse(monthYear, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        throw new BadRequestException($"Không thể parse tháng năm: {monthYear}. Định dạng cần là MM/yyyy hoặc M/yyyy");
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        var errors = importErrors
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

internal class TrimmingMaintainUnitPriceImportDto
{
    public string EquipmentCode { get; set; } = string.Empty;
    public string StartMonth { get; set; } = string.Empty;
    public string EndMonth { get; set; } = string.Empty;
    public bool IsDeleteRequest { get; set; }
    public Dictionary<Guid, TrimmingPartEquipmentData?> PartData { get; set; } = new(); // Key is PartId
}

internal readonly record struct TrimmingMaintainUnitPriceLookupKey(Guid EquipmentId, DateOnly StartMonth, DateOnly EndMonth);

internal class TrimmingMaintainUnitPriceImportItem
{
    public string EquipmentCode { get; set; } = string.Empty;
    public Guid EquipmentId { get; set; }
    public DateOnly StartMonth { get; set; }
    public DateOnly EndMonth { get; set; }
    public IList<Domain.Entities.Pricing.MaintainUnitPriceEquipment> Parts { get; set; } = new List<Domain.Entities.Pricing.MaintainUnitPriceEquipment>();
    public bool IsDeleteRequest { get; set; }
}

internal struct TrimmingPartEquipmentData
{
    public double Quantity { get; set; }
    public decimal AverageMonthlyTunnelProduction { get; set; }
    public decimal ReplacementTimeStandard { get; set; }
}

internal class TrimmingEquipmentImportState
{
    public required TrimmingMaintainUnitPriceImportDto Dto { get; set; }
    public int TotalParts { get; set; }
    public int FilledParts { get; set; }
}



