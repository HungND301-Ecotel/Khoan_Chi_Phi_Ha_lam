using Application.Common.Exceptions;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using ClosedXML.Excel;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record ExportLumpSumFinalSettlementMonthExcelQuery(
    string Month,
    string Year,
    string? ProcessGroupId,
    string? Search) : IRequest<ExportLumpSumFinalSettlementMonthExcelResponse>;

public record ExportLumpSumFinalSettlementMonthExcelResponse(byte[] FileBytes, string FileName);

public class ExportLumpSumFinalSettlementMonthExcelQueryHandler(IMediator mediator)
    : IRequestHandler<ExportLumpSumFinalSettlementMonthExcelQuery, ExportLumpSumFinalSettlementMonthExcelResponse>
{
    private const string SheetName = "Bang thanh toan";
    private const int TotalColumns = 12;
    private const int HeaderTopRow = 7;
    private const int HeaderBottomRow = 8;

    public async Task<ExportLumpSumFinalSettlementMonthExcelResponse> Handle(
        ExportLumpSumFinalSettlementMonthExcelQuery request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Month, out var month) || month is < 1 or > 12)
        {
            throw new BadRequestException("Invalid month");
        }

        if (!int.TryParse(request.Year, out var year) || year < 1)
        {
            throw new BadRequestException("Invalid year");
        }

        var response = await mediator.Send(
            new GetLumpSumFinalSettlementListQuery(
                request.Month,
                request.Year,
                request.ProcessGroupId ?? string.Empty),
            cancellationToken);

        var groupedRows = GroupByProcessGroup(response.Items);
        var filteredRows = ApplySearch(groupedRows, request.Search);
        var fileBytes = BuildWorkbook(filteredRows, month, year);
        var fileName = $"bao-cao-thanh-toan-thang-{month}-nam-{year}.xlsx";

        return new ExportLumpSumFinalSettlementMonthExcelResponse(fileBytes, fileName);
    }

    private static List<ExportRow> GroupByProcessGroup(IReadOnlyCollection<LumpSumFinalSettlementDto> items)
    {
        var groups = new List<(string Key, List<LumpSumFinalSettlementDto> Items)>();

        foreach (var item in items)
        {
            var key = item.ProcessGroupId != Guid.Empty
                ? item.ProcessGroupId.ToString()
                : $"{item.ProcessGroupCode}|{item.ProcessGroupName}";

            var existingIndex = groups.FindIndex(x => x.Key == key);
            if (existingIndex >= 0)
            {
                groups[existingIndex].Items.Add(item);
                continue;
            }

            groups.Add((key, [item]));
        }

        var result = new List<ExportRow>();
        var stt = 1;

        foreach (var (_, groupItems) in groups)
        {
            var first = groupItems[0];
            var groupTitle = string.Join(
                " - ",
                new[] { first.ProcessGroupCode?.Trim(), first.ProcessGroupName?.Trim() }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (string.IsNullOrWhiteSpace(groupTitle))
            {
                groupTitle = "Chua phan nhom";
            }

            result.Add(new ExportRow
            {
                SttLabel = stt.ToString(),
                ProductName = groupTitle,
                PlannedQuantity = groupItems.Sum(x => x.PlannedQuantity),
                ActualQuantity = groupItems.Sum(x => x.ActualQuantity),
                MaterialsTotalAmount = groupItems.Sum(x => x.Materials?.TotalAmount ?? 0),
                MaintainsTotalAmount = groupItems.Sum(x => x.Maintains?.TotalAmount ?? 0),
                ElectricitiesTotalAmount = groupItems.Sum(x => x.Electricities?.TotalAmount ?? 0),
                TotalAmount = groupItems.Sum(x => x.TotalAmount),
                IsBold = true,
                IsProcessGroupRow = true
            });

            for (var index = 0; index < groupItems.Count; index++)
            {
                var item = groupItems[index];
                result.Add(new ExportRow
                {
                    SttLabel = $"{stt}.{index + 1}",
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    UnitOfMeasureName = item.UnitOfMeasureName,
                    PlannedQuantity = item.PlannedQuantity,
                    ActualQuantity = item.ActualQuantity,
                    MaterialsUnitPrice = item.Materials?.UnitPrice,
                    MaterialsTotalAmount = item.Materials?.TotalAmount ?? 0,
                    MaintainsUnitPrice = item.Maintains?.UnitPrice,
                    MaintainsTotalAmount = item.Maintains?.TotalAmount ?? 0,
                    ElectricitiesUnitPrice = item.Electricities?.UnitPrice,
                    ElectricitiesTotalAmount = item.Electricities?.TotalAmount ?? 0,
                    TotalAmount = item.TotalAmount
                });
            }

            stt++;
        }

        return result;
    }

    private static List<ExportRow> ApplySearch(IReadOnlyList<ExportRow> rows, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return [.. rows];
        }

        var query = search.Trim().ToLowerInvariant();
        return rows.Where(row =>
        {
            var keywords = string.Join(
                ' ',
                new[] { row.ProductCode, row.ProductName, row.UnitOfMeasureName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)))
                .ToLowerInvariant();

            return keywords.Contains(query);
        }).ToList();
    }

    private static byte[] BuildWorkbook(IReadOnlyList<ExportRow> rows, int month, int year)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(SheetName);
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 12;
        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.SheetView.FreezeRows(HeaderBottomRow + 1);

        ConfigureColumnWidths(worksheet);
        WriteReportHeader(worksheet, month, year);
        WriteHeader(worksheet);
        WriteSummaryRow(worksheet, rows);

        var currentRow = HeaderBottomRow + 2;
        foreach (var row in rows)
        {
            WriteDataRow(worksheet, currentRow, row);
            currentRow++;
        }

        if (rows.Count == 0)
        {
            worksheet.Range(currentRow, 1, currentRow, TotalColumns).Merge();
            worksheet.Cell(currentRow, 1).Value = "Khong co du lieu";
            worksheet.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ApplyRowBorders(worksheet, currentRow);
            currentRow++;
        }

        WriteReportTotal(worksheet, currentRow + 1, rows.Sum(x => x.TotalAmount));
        WriteReportFooter(worksheet, currentRow + 4, month, year);

        var tableRange = worksheet.Range(HeaderTopRow, 1, Math.Max(currentRow - 1, HeaderTopRow), TotalColumns);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        tableRange.Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteReportHeader(IXLWorksheet worksheet, int month, int year)
    {
        worksheet.Range(1, 1, 2, 5).Merge();
        worksheet.Cell(1, 1).Value = "CONG TY CO PHAN THAN HA LAM - VINACOMIN";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Cell(1, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        worksheet.Range(3, 1, 3, 5).Merge();
        worksheet.Cell(3, 1).Value = "CONG TRUONG KHAI THAC 1";
        worksheet.Cell(3, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(3, 1).Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        worksheet.Range(1, 10, 1, 12).Merge();
        worksheet.Cell(1, 10).Value = "DVT: Dong";
        worksheet.Cell(1, 10).Style.Font.Bold = true;
        worksheet.Cell(1, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        worksheet.Range(2, 10, 2, 12).Merge();
        worksheet.Cell(2, 10).Value = "Bang so: 05";
        worksheet.Cell(2, 10).Style.Font.Bold = true;
        worksheet.Cell(2, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        worksheet.Range(4, 1, 4, TotalColumns).Merge();
        worksheet.Cell(4, 1).Value = "BANG THANH TOAN";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Font.FontSize = 16;
        worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(5, 1, 5, TotalColumns).Merge();
        worksheet.Cell(5, 1).Value = $"THANG {month} NAM {year}";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 1).Style.Font.FontSize = 14;
        worksheet.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        worksheet.Range(HeaderTopRow, 1, HeaderBottomRow, 1).Merge().Value = "STT";
        worksheet.Range(HeaderTopRow, 2, HeaderBottomRow, 2).Merge().Value = "SAN PHAM";
        worksheet.Range(HeaderTopRow, 3, HeaderBottomRow, 3).Merge().Value = "DVT";
        worksheet.Range(HeaderTopRow, 4, HeaderBottomRow, 4).Merge().Value = "KH";
        worksheet.Range(HeaderTopRow, 5, HeaderBottomRow, 5).Merge().Value = "TH";

        worksheet.Range(HeaderTopRow, 6, HeaderTopRow, 7).Merge().Value = "VAT LIEU";
        worksheet.Range(HeaderTopRow, 8, HeaderTopRow, 9).Merge().Value = "SUA CHUA THUONG XUYEN";
        worksheet.Range(HeaderTopRow, 10, HeaderTopRow, 11).Merge().Value = "DONG LUC (DIEN NANG)";
        worksheet.Range(HeaderTopRow, 12, HeaderBottomRow, 12).Merge().Value = "TONG";

        worksheet.Cell(HeaderBottomRow, 6).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 7).Value = "THANH TIEN";
        worksheet.Cell(HeaderBottomRow, 8).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 9).Value = "THANH TIEN";
        worksheet.Cell(HeaderBottomRow, 10).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 11).Value = "THANH TIEN";

        var headerRange = worksheet.Range(HeaderTopRow, 1, HeaderBottomRow, TotalColumns);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
    }

    private static void WriteSummaryRow(IXLWorksheet worksheet, IReadOnlyList<ExportRow> rows)
    {
        var summaryRow = HeaderBottomRow + 1;
        var visibleRows = rows.Where(x => !x.IsProcessGroupRow).ToList();

        worksheet.Row(summaryRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
        worksheet.Row(summaryRow).Style.Font.Bold = true;

        SetNumberCell(worksheet.Cell(summaryRow, 4), visibleRows.Sum(x => x.PlannedQuantity ?? 0), 3);
        SetNumberCell(worksheet.Cell(summaryRow, 5), visibleRows.Sum(x => x.ActualQuantity ?? 0), 3);
        SetNumberCell(worksheet.Cell(summaryRow, 7), visibleRows.Sum(x => x.MaterialsTotalAmount));
        SetNumberCell(worksheet.Cell(summaryRow, 9), visibleRows.Sum(x => x.MaintainsTotalAmount));
        SetNumberCell(worksheet.Cell(summaryRow, 11), visibleRows.Sum(x => x.ElectricitiesTotalAmount));
        SetNumberCell(worksheet.Cell(summaryRow, 12), visibleRows.Sum(x => x.TotalAmount));

        ApplyRowBorders(worksheet, summaryRow);
    }

    private static void WriteDataRow(IXLWorksheet worksheet, int rowNumber, ExportRow row)
    {
        worksheet.Cell(rowNumber, 1).Value = row.SttLabel ?? string.Empty;
        worksheet.Cell(rowNumber, 2).Value = row.ProductName ?? string.Empty;
        worksheet.Cell(rowNumber, 3).Value = row.UnitOfMeasureName ?? string.Empty;
        SetNumberCell(worksheet.Cell(rowNumber, 4), row.PlannedQuantity, 3);
        SetNumberCell(worksheet.Cell(rowNumber, 5), row.ActualQuantity, 3);
        SetNumberCell(worksheet.Cell(rowNumber, 6), row.MaterialsUnitPrice);
        SetNumberCell(worksheet.Cell(rowNumber, 7), row.MaterialsTotalAmount);
        SetNumberCell(worksheet.Cell(rowNumber, 8), row.MaintainsUnitPrice);
        SetNumberCell(worksheet.Cell(rowNumber, 9), row.MaintainsTotalAmount);
        SetNumberCell(worksheet.Cell(rowNumber, 10), row.ElectricitiesUnitPrice);
        SetNumberCell(worksheet.Cell(rowNumber, 11), row.ElectricitiesTotalAmount);
        SetNumberCell(worksheet.Cell(rowNumber, 12), row.TotalAmount);

        if (row.IsBold)
        {
            worksheet.Row(rowNumber).Style.Font.Bold = true;
        }

        ApplyRowBorders(worksheet, rowNumber);
    }

    private static void WriteReportTotal(IXLWorksheet worksheet, int rowNumber, double totalValue)
    {
        worksheet.Range(rowNumber, 1, rowNumber, TotalColumns - 1).Merge();
        worksheet.Cell(rowNumber, 1).Value = "Tong gia tri bang:";
        worksheet.Cell(rowNumber, 1).Style.Font.Italic = true;
        worksheet.Cell(rowNumber, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        SetNumberCell(worksheet.Cell(rowNumber, TotalColumns), totalValue);
        worksheet.Cell(rowNumber, TotalColumns).Style.Font.Italic = true;
    }

    private static void WriteReportFooter(IXLWorksheet worksheet, int startRow, int month, int year)
    {
        var signDate = DateTime.Now;

        worksheet.Range(startRow, 8, startRow, 12).Merge();
        worksheet.Cell(startRow, 8).Value = $"Ha Lam, ngay {signDate:dd} thang {signDate:MM} nam {signDate:yyyy}";
        worksheet.Cell(startRow, 8).Style.Font.Italic = true;
        worksheet.Cell(startRow, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(startRow + 1, 1, startRow + 1, 6).Merge();
        worksheet.Cell(startRow + 1, 1).Value = "DAI DIEN BEN NHAN KHOAN";
        worksheet.Range(startRow + 1, 7, startRow + 1, 12).Merge();
        worksheet.Cell(startRow + 1, 7).Value = "DAI DIEN BEN GIAO KHOAN";

        worksheet.Range(startRow + 2, 1, startRow + 2, 2).Merge();
        worksheet.Cell(startRow + 2, 1).Value = "NGUOI LAP";
        worksheet.Range(startRow + 2, 3, startRow + 2, 4).Merge();
        worksheet.Cell(startRow + 2, 3).Value = "QUAN DOC";
        worksheet.Range(startRow + 2, 5, startRow + 2, 6).Merge();
        worksheet.Cell(startRow + 2, 5).Value = "PHONG KTTC";
        worksheet.Range(startRow + 2, 7, startRow + 2, 8).Merge();
        worksheet.Cell(startRow + 2, 7).Value = "PHONG KH";
        worksheet.Range(startRow + 2, 9, startRow + 2, 12).Merge();
        worksheet.Cell(startRow + 2, 9).Value = "KT.GIAM DOC / PHO GIAM DOC";

        worksheet.Range(startRow + 5, 1, startRow + 5, 12).Merge();
        worksheet.Cell(startRow + 5, 1).Value = $"Bieu mau thang {month}/{year}";
        worksheet.Cell(startRow + 5, 1).Style.Font.Italic = true;

        var footerRange = worksheet.Range(startRow + 1, 1, startRow + 2, 12);
        footerRange.Style.Font.Bold = true;
        footerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void ApplyRowBorders(IXLWorksheet worksheet, int rowNumber)
    {
        var rowRange = worksheet.Range(rowNumber, 1, rowNumber, TotalColumns);
        rowRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        rowRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        rowRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        rowRange.Style.Alignment.WrapText = true;
    }

    private static void ConfigureColumnWidths(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Width = 8;
        worksheet.Column(2).Width = 34;
        worksheet.Column(3).Width = 10;
        worksheet.Column(4).Width = 12;
        worksheet.Column(5).Width = 12;
        worksheet.Column(6).Width = 14;
        worksheet.Column(7).Width = 16;
        worksheet.Column(8).Width = 14;
        worksheet.Column(9).Width = 16;
        worksheet.Column(10).Width = 14;
        worksheet.Column(11).Width = 16;
        worksheet.Column(12).Width = 18;
    }

    private static void SetNumberCell(IXLCell cell, double? value, int maximumFractionDigits = 0)
    {
        if (!value.HasValue)
        {
            cell.Value = string.Empty;
            return;
        }

        if (maximumFractionDigits > 0)
        {
            cell.Value = Math.Round(value.Value, maximumFractionDigits, MidpointRounding.AwayFromZero);
            cell.Style.NumberFormat.Format = "#,##0.###";
            return;
        }

        cell.Value = Math.Round(value.Value, 0, MidpointRounding.AwayFromZero);
        cell.Style.NumberFormat.Format = "#,##0";
    }

    private sealed class ExportRow
    {
        public string? SttLabel { get; init; }
        public string? ProductCode { get; init; }
        public string? ProductName { get; init; }
        public string? UnitOfMeasureName { get; init; }
        public double? PlannedQuantity { get; init; }
        public double? ActualQuantity { get; init; }
        public double? MaterialsUnitPrice { get; init; }
        public double MaterialsTotalAmount { get; init; }
        public double? MaintainsUnitPrice { get; init; }
        public double MaintainsTotalAmount { get; init; }
        public double? ElectricitiesUnitPrice { get; init; }
        public double ElectricitiesTotalAmount { get; init; }
        public double TotalAmount { get; init; }
        public bool IsBold { get; init; }
        public bool IsProcessGroupRow { get; init; }
    }
}
