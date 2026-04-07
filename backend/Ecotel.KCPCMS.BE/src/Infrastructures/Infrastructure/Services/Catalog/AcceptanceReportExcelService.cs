using Application.Common.Exceptions;
using Application.Dto.Catalog.AcceptanceReport;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using System.Globalization;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Services;

public class AcceptanceReportExcelService : IAcceptanceReportExcelService
{
    private const string UploadFolder = "Uploads";
    private const string AcceptanceReportFolder = "AcceptanceReports";

    public async Task<UploadAcceptanceReportResponseDto> ProcessExcelFileAsync(
        Guid OutputId,
        Stream fileStream,
        string fileName,
        IEnumerable<Material> materialsInDb,
        IEnumerable<Part> partsIsInDb)
    {
        // Validate file extension
        if (!fileName.EndsWith(".xlsx") && !fileName.EndsWith(".xls"))
        {
            throw new BadRequestException(CustomResponseMessage.UnsupportedFileFormat);
        }

        var materials = materialsInDb.ToList();
        var parts = partsIsInDb.ToList();

        try
        {
            using (var workbook = new XLWorkbook(fileStream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    throw new BadRequestException(CustomResponseMessage.ExcelFileHasNoWorksheet);
                }

                var acceptanceReports = new List<AcceptanceReportItemDto>();
                var importErrors = new List<string>();

                foreach (var row in worksheet.RowsUsed())
                {
                    var rowNumber = row.RowNumber();
                    if (rowNumber == 1)
                    {
                        continue; // Skip header row
                    }

                    var idStr = row.Cell(1).Value.ToString()?.Trim();
                    var materialCode = row.Cell(2).Value.ToString()?.Trim();
                    var quantityReceived = row.Cell(3).Value.ToString()?.Trim();
                    var quantityDispensed = row.Cell(4).Value.ToString()?.Trim();

                    // Parse Id (optional - can be empty for new items)
                    Guid? reportItemId = null;
                    if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var parsedId))
                    {
                        reportItemId = parsedId;
                    }
                    else if (!string.IsNullOrWhiteSpace(idStr))
                    {
                        importErrors.Add($"Id không đúng định dạng Guid ở dòng {rowNumber}.");
                    }

                    // Validate data
                    if (string.IsNullOrWhiteSpace(materialCode))
                    {
                        importErrors.Add($"Mã vật tư không thể trống ở dòng {rowNumber}.");
                        continue;
                    }

                    if (!TryParseQuantity(quantityReceived, out var receivedValue))
                    {
                        importErrors.Add($"Số lượng nhập phải là số ở dòng {rowNumber}.");
                        continue;
                    }

                    if (!TryParseQuantity(quantityDispensed, out var dispensedValue))
                    {
                        importErrors.Add($"Số lượng xuất phải là số ở dòng {rowNumber}.");
                        continue;
                    }

                    // Find material in database
                    var material = materials.FirstOrDefault(m =>
                        m.Code.Value.Equals(materialCode, StringComparison.OrdinalIgnoreCase));

                    string unitOfMeasureName = "N/A";

                    // If material not found, check part
                    var materialsOrPartsId = material?.Id;
                    var type = AcceptanceReportItemType.Material;
                    var itemType = (int)(material?.MaterialType ?? MaterialType.MaterialOutContract);
                    if (material == null)
                    {
                        var part = parts.FirstOrDefault(p =>
                            p.Code.Value.Equals(materialCode, StringComparison.OrdinalIgnoreCase));
                        type = AcceptanceReportItemType.Part;
                        itemType = (int)(part?.Type ?? PartType.OtherPart);
                        materialsOrPartsId = part?.Id;
                        if (part == null)
                        {
                            importErrors.Add($"Không tìm thấy vật tư/phụ tùng: '{materialCode}' ở dòng {rowNumber}.");
                            continue;
                        }

                        unitOfMeasureName = part.UnitOfMeasure?.Name ?? "N/A";
                    }
                    else
                    {
                        unitOfMeasureName = material.UnitOfMeasure?.Name ?? "N/A";
                    }

                    acceptanceReports.Add(new AcceptanceReportItemDto
                    {
                        ReportItemId = reportItemId,
                        MaterialOrPartId = materialsOrPartsId ?? Guid.Empty,
                        Type = type,
                        ItemType = (ItemType)itemType,
                        MaterialCode = materialCode,
                        UnitOfMeasureName = unitOfMeasureName,
                        IssuedQuantity = receivedValue,
                        ShippedQuantity = dispensedValue
                    });
                }

                ThrowIfImportErrors(importErrors);

                if (!acceptanceReports.Any())
                {
                    throw new BadRequestException(CustomResponseMessage.ExcelFileHasNoValidData);
                }

                return new UploadAcceptanceReportResponseDto
                {
                    FilePath = "",
                    AcceptanceReports = acceptanceReports.OrderBy(a => a.MaterialCode).ToList()
                };
            }
        }
        catch (Exception ex) when (ex is not BadRequestException && ex is not ExcelImportException)
        {
            throw new BadRequestException(CustomResponseMessage.ErrorProcessingExcelFile);
        }
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
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue))
        {
            return true;
        }

        return double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out parsedValue);
    }
}
