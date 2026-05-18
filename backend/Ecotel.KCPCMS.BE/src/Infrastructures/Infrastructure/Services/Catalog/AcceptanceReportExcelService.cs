using System.Globalization;
using System.Text;
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
        IEnumerable<Part> partsInDb)
    {
        if (!fileName.EndsWith(".xlsx") && !fileName.EndsWith(".xls"))
        {
            throw new BadRequestException(CustomResponseMessage.UnsupportedFileFormat);
        }

        var materials = materialsInDb.ToList();
        var parts = partsInDb
            .Where(p => p.Code?.Value != null)
            .ToList();
        var materialByNormalizedCode = materials
            .Where(m => m.Code?.Value != null)
            .GroupBy(m => NormalizeCode(m.Code!.Value))
            .ToDictionary(g => g.Key, g => g.First());
        var partByNormalizedCode = parts
            .Where(p => p.Code?.Value != null)
            .GroupBy(p => NormalizeCode(p.Code!.Value))
            .ToDictionary(g => g.Key, g => g.First());

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                throw new BadRequestException(CustomResponseMessage.ExcelFileHasNoWorksheet);
            }

            var acceptanceReports = new List<AcceptanceReportItemDto>();
            var unresolvedAcceptanceReports = new List<UnresolvedAcceptanceReportItemDto>();
            var fatalErrors = new List<string>();

            foreach (var row in worksheet.RowsUsed())
            {
                var rowNumber = row.RowNumber();
                if (rowNumber == 1)
                {
                    continue;
                }

                var idStr = row.Cell(1).Value.ToString()?.Trim();
                var materialCode = row.Cell(2).Value.ToString()?.Trim();
                var normalizedMaterialCode = NormalizeCode(materialCode);
                var quantityReceived = row.Cell(3).Value.ToString()?.Trim();
                var quantityDispensed = row.Cell(4).Value.ToString()?.Trim();

                Guid? reportItemId = null;
                if (!string.IsNullOrWhiteSpace(idStr) && Guid.TryParse(idStr, out var parsedId))
                {
                    reportItemId = parsedId;
                }
                else if (!string.IsNullOrWhiteSpace(idStr))
                {
                    fatalErrors.Add($"Id không đúng định dạng Guid ở dòng {rowNumber}.");
                }

                if (string.IsNullOrWhiteSpace(materialCode))
                {
                    fatalErrors.Add($"Mã vật tư không thể trống ở dòng {rowNumber}.");
                    continue;
                }

                if (!TryParseQuantity(quantityReceived, out var receivedValue))
                {
                    fatalErrors.Add($"Số lượng nhập phải là số ở dòng {rowNumber}.");
                    continue;
                }

                if (!TryParseQuantity(quantityDispensed, out var dispensedValue))
                {
                    fatalErrors.Add($"Số lượng xuất phải là số ở dòng {rowNumber}.");
                    continue;
                }

                materialByNormalizedCode.TryGetValue(normalizedMaterialCode, out var material);

                var type = AcceptanceReportItemType.Material;
                var itemType = (int)(material?.MaterialType ?? MaterialType.MaterialOutContract);
                Guid? materialId = null;
                Guid? partId = null;
                PartType? partType = null;
                string unitOfMeasureName;
                string? materialName = null;
                string? partName = null;

                if (material != null)
                {
                    materialId = material.Id;
                    materialName = material.Name;
                    unitOfMeasureName = material.UnitOfMeasure?.Name ?? "N/A";
                }
                else
                {
                    partByNormalizedCode.TryGetValue(normalizedMaterialCode, out var part);

                    if (part == null)
                    {
                        unresolvedAcceptanceReports.Add(new UnresolvedAcceptanceReportItemDto
                        {
                            RowNumber = rowNumber,
                            ReportItemId = reportItemId,
                            MaterialCode = materialCode,
                            MaterialName = null,
                            IssuedQuantity = receivedValue,
                            ShippedQuantity = dispensedValue,
                            UnresolvedReason = $"Không tìm thấy vật tư: '{materialCode}' ở dòng {rowNumber}."
                        });
                        continue;
                    }

                    type = AcceptanceReportItemType.Part;
                    itemType = (int)part.Type;
                    partType = part.Type;
                    partId = part.Id;
                    partName = part.Name;
                    unitOfMeasureName = part.UnitOfMeasure?.Name ?? "N/A";
                }

                acceptanceReports.Add(new AcceptanceReportItemDto
                {
                    ReportItemId = reportItemId,
                    RowNumber = rowNumber,
                    TrackedMaterialId = materialId ?? partId,
                    MaterialId = materialId,
                    PartId = partId,
                    Type = type,
                    ItemType = (ItemType)itemType,
                    PartType = partType,
                    MaterialCode = materialCode,
                    MaterialName = materialName,
                    TrackedMaterialCode = materialCode,
                    TrackedMaterialName = materialName ?? partName,
                    PartName = partName,
                    UnitOfMeasureName = unitOfMeasureName,
                    IssuedQuantity = receivedValue,
                    ShippedQuantity = dispensedValue
                });
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
