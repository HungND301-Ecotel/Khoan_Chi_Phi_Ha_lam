using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Application.Common.Exceptions;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Services;

public class AcceptanceReportExcelService : IAcceptanceReportExcelService
{
    public async Task<UploadAcceptanceReportResponseDto> ProcessExcelFileAsync(
        Guid outputId,
        Stream fileStream,
        string fileName,
        IEnumerable<Material> materialsInDb,
        DateOnly? periodMonth = null)
    {
        if (!fileName.EndsWith(".xlsx") && !fileName.EndsWith(".xls"))
        {
            throw new BadRequestException(CustomResponseMessage.UnsupportedFileFormat);
        }

        var materials = materialsInDb.ToList();
        var materialByNormalizedCode = materials
            .Where(m => m.Code?.Value != null)
            .GroupBy(m => NormalizeCode(m.Code!.Value))
            .ToDictionary(g => g.Key, g => g.First());

        var year = periodMonth?.Year ?? DateTime.Now.Year;
        var month = periodMonth?.Month ?? DateTime.Now.Month;

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var allWorksheets = workbook.Worksheets.ToList();
            if (!allWorksheets.Any())
            {
                throw new BadRequestException(CustomResponseMessage.ExcelFileHasNoWorksheet);
            }

            // Detect day sheets
            var daySheets = new List<(IXLWorksheet Worksheet, DateOnly SheetDate)>();
            foreach (var ws in allWorksheets)
            {
                if (TryParseDayFromSheetName(ws.Name, year, month, out var sDate))
                {
                    daySheets.Add((ws, sDate));
                }
            }

            List<(IXLWorksheet Worksheet, DateOnly? SheetDate)> sheetsToProcess;
            if (daySheets.Any())
            {
                sheetsToProcess = daySheets
                    .OrderBy(x => x.SheetDate)
                    .Select(x => (x.Worksheet, (DateOnly?)x.SheetDate))
                    .ToList();
            }
            else
            {
                var visibleSheets = allWorksheets.Where(w => w.Visibility == XLWorksheetVisibility.Visible).ToList();
                var fallbackList = visibleSheets.Any() ? visibleSheets : allWorksheets;
                sheetsToProcess = fallbackList.Select(w => (w, (DateOnly?)null)).ToList();
            }

            var acceptanceReports = new List<AcceptanceReportItemDto>();
            var unresolvedAcceptanceReports = new List<UnresolvedAcceptanceReportItemDto>();
            var fatalErrors = new List<string>();
            int globalRowCounter = 1;

            foreach (var (worksheet, sheetDate) in sheetsToProcess)
            {
                var rowsUsed = worksheet.RowsUsed().ToList();
                if (!rowsUsed.Any())
                {
                    continue;
                }

                foreach (var row in rowsUsed)
                {
                    var rowNumber = row.RowNumber();
                    if (rowNumber == 1)
                    {
                        continue;
                    }

                    var idStr = row.Cell(1).Value.ToString()?.Trim();
                    var documentNumber = row.Cell(2).Value.ToString()?.Trim();
                    var postingDateCell = row.Cell(3);
                    var materialCode = row.Cell(4).Value.ToString()?.Trim();
                    var normalizedMaterialCode = NormalizeCode(materialCode);
                    var quantityReceived = row.Cell(5).Value.ToString()?.Trim();
                    var quantityDispensed = row.Cell(6).Value.ToString()?.Trim();

                    // Skip completely empty rows
                    if (string.IsNullOrWhiteSpace(idStr) &&
                        string.IsNullOrWhiteSpace(documentNumber) &&
                        postingDateCell.IsEmpty() &&
                        string.IsNullOrWhiteSpace(materialCode) &&
                        string.IsNullOrWhiteSpace(quantityReceived) &&
                        string.IsNullOrWhiteSpace(quantityDispensed))
                    {
                        continue;
                    }

                    var sheetSuffix = sheetsToProcess.Count > 1 ? $" (sheet '{worksheet.Name}')" : string.Empty;

                    if (!string.IsNullOrEmpty(documentNumber) && documentNumber.Length > 255)
                    {
                        fatalErrors.Add($"Số chứng từ không được vượt quá 255 ký tự ở dòng {rowNumber}{sheetSuffix}.");
                        continue;
                    }

                    if (!TryParsePostingDate(postingDateCell, out var cellPostingDate))
                    {
                        fatalErrors.Add($"Ngày vào sổ không đúng định dạng ở dòng {rowNumber}{sheetSuffix}.");
                        continue;
                    }

                    var postingDate = cellPostingDate ?? sheetDate;

                    Guid? reportItemId = null;
                    if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var parsedId))
                    {
                        reportItemId = parsedId;
                    }
                    else if (!string.IsNullOrWhiteSpace(idStr))
                    {
                        fatalErrors.Add($"Id không đúng định dạng Guid ở dòng {rowNumber}{sheetSuffix}.");
                    }

                    if (string.IsNullOrWhiteSpace(materialCode))
                    {
                        fatalErrors.Add($"Mã vật tư không thể trống ở dòng {rowNumber}{sheetSuffix}.");
                        continue;
                    }

                    if (!TryParseQuantity(quantityReceived, out var receivedValue))
                    {
                        fatalErrors.Add($"Số lượng nhập phải là số ở dòng {rowNumber}{sheetSuffix}.");
                        continue;
                    }

                    if (!TryParseQuantity(quantityDispensed, out var dispensedValue))
                    {
                        fatalErrors.Add($"Số lượng xuất phải là số ở dòng {rowNumber}{sheetSuffix}.");
                        continue;
                    }

                    var currentRowIndex = globalRowCounter++;

                    materialByNormalizedCode.TryGetValue(normalizedMaterialCode, out var material);

                    if (material == null)
                    {
                        unresolvedAcceptanceReports.Add(new UnresolvedAcceptanceReportItemDto
                        {
                            RowNumber = currentRowIndex,
                            SheetName = worksheet.Name,
                            ReportItemId = reportItemId,
                            DocumentNumber = documentNumber,
                            PostingDate = postingDate,
                            MaterialCode = materialCode,
                            MaterialName = null,
                            IssuedQuantity = receivedValue,
                            ShippedQuantity = dispensedValue,
                            UnresolvedReason = $"Không tìm thấy vật tư: '{materialCode}' ở dòng {rowNumber}{sheetSuffix}."
                        });
                        continue;
                    }

                    var type = material.MaterialType == MaterialType.MaterialOutContract
                        ? AcceptanceReportItemType.Material
                        : AcceptanceReportItemType.Part;
                    var itemType = (int)material.MaterialType;
                    Guid? materialId = type == AcceptanceReportItemType.Material ? material.Id : null;
                    Guid? partId = type == AcceptanceReportItemType.Part ? material.Id : null;
                    PartType? partType = type == AcceptanceReportItemType.Part ? PartType.Part : null;
                    var unitOfMeasureName = material.UnitOfMeasure?.Name ?? "N/A";
                    var materialName = material.Name;

                    acceptanceReports.Add(new AcceptanceReportItemDto
                    {
                        ReportItemId = reportItemId,
                        RowNumber = currentRowIndex,
                        SheetName = worksheet.Name,
                        DocumentNumber = documentNumber,
                        PostingDate = postingDate,
                        TrackedMaterialId = materialId ?? partId,
                        MaterialId = materialId,
                        PartId = partId,
                        Type = type,
                        ItemType = (ItemType)itemType,
                        PartType = partType,
                        MaterialCode = materialCode,
                        MaterialName = materialName,
                        TrackedMaterialCode = materialCode,
                        TrackedMaterialName = materialName,
                        PartCode = partId.HasValue ? materialCode : null,
                        PartName = partId.HasValue ? materialName : null,
                        UnitOfMeasureName = unitOfMeasureName,
                        IssuedQuantity = receivedValue,
                        ShippedQuantity = dispensedValue
                    });
                }
            }

            ThrowIfImportErrors(fatalErrors);

            if (!acceptanceReports.Any() && !unresolvedAcceptanceReports.Any())
            {
                throw new BadRequestException(CustomResponseMessage.ExcelFileHasNoValidData);
            }

            return new UploadAcceptanceReportResponseDto
            {
                FilePath = "",
                AcceptanceReports = acceptanceReports,
                UnresolvedAcceptanceReports = unresolvedAcceptanceReports
            };
        }
        catch (Exception ex) when (ex is not BadRequestException && ex is not ExcelImportException)
        {
            throw new BadRequestException(CustomResponseMessage.ErrorProcessingExcelFile);
        }
    }

    private static bool TryParseDayFromSheetName(string sheetName, int year, int month, out DateOnly sheetDate)
    {
        sheetDate = default;
        if (string.IsNullOrWhiteSpace(sheetName))
        {
            return false;
        }

        var trimmed = sheetName.Trim();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // 1. Direct integer e.g., "1", "01", "31"
        if (int.TryParse(trimmed, out var day) && day >= 1 && day <= daysInMonth)
        {
            sheetDate = new DateOnly(year, month, day);
            return true;
        }

        // 2. Pattern "Ngày 01", "Ngay 1", "Day 01", "D01"
        var match = Regex.Match(trimmed, @"^(?:ngày|ngay|day|d)\s*(\d{1,2})$", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var prefixDay) && prefixDay >= 1 && prefixDay <= daysInMonth)
        {
            sheetDate = new DateOnly(year, month, prefixDay);
            return true;
        }

        // 3. Full date format e.g. "01-09-2026", "01/09/2026", "2026-09-01"
        if (DateOnly.TryParseExact(trimmed, ["d/M/yyyy", "dd/MM/yyyy", "d-M-yyyy", "dd-MM-yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var fullDate))
        {
            sheetDate = fullDate;
            return true;
        }

        return false;
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        var errors = importErrors
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ExcelImportException(errors);
    }

    private static bool TryParseQuantity(string? value, out double parsedValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsedValue = 0;
            return true;
        }

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue))
        {
            return true;
        }

        return double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedValue);
    }

    private static bool TryParsePostingDate(IXLCell cell, out DateOnly? postingDate)
    {
        postingDate = null;

        var rawValue = cell.Value.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return true;
        }

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            postingDate = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (DateOnly.TryParseExact(
                rawValue,
                ["d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "M/d/yyyy", "MM/dd/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedDate))
        {
            postingDate = parsedDate;
            return true;
        }

        if (DateOnly.TryParse(rawValue, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
        {
            postingDate = parsedDate;
            return true;
        }

        return false;
    }

    private static string NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(char.ToUpperInvariant(character));
            previousWasWhitespace = false;
        }

        return builder.ToString();
    }
}
