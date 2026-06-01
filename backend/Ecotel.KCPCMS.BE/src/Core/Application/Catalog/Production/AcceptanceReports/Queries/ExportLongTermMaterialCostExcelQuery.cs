using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using ClosedXML.Excel;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record ExportLongTermMaterialCostExcelQuery(
    Guid AcceptanceReportId,
    string? Month,
    string? Year,
    Guid? ProcessGroupId) : IRequest<ExportLongTermMaterialCostExcelResponse>;

public record ExportLongTermMaterialCostExcelResponse(byte[] FileBytes, string FileName);

public class ExportLongTermMaterialCostExcelQueryHandler(IUnitOfWork unitOfWork, IMediator mediator)
    : IRequestHandler<ExportLongTermMaterialCostExcelQuery, ExportLongTermMaterialCostExcelResponse>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<ExportLongTermMaterialCostExcelResponse> Handle(ExportLongTermMaterialCostExcelQuery request, CancellationToken cancellationToken)
    {
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: x => x.Id == request.AcceptanceReportId,
            include: q => q.Include(x => x.ProductionOutput),
            disableTracking: true);

        if (acceptanceReport == null)
        {
            throw new NotFoundException("Acceptance report not found.");
        }

        var detail = await mediator.Send(new GetDetailLongTermTrackingQuery(request.AcceptanceReportId), cancellationToken);
        var flattenedItems = FlattenItems(detail);

        if (request.ProcessGroupId.HasValue)
        {
            flattenedItems = flattenedItems
                .Where(x => x.ProcessGroupId == request.ProcessGroupId)
                .ToList();
        }

        var startMonth = acceptanceReport.ProductionOutput?.StartMonth ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var (resolvedMonth, resolvedYear) = ResolveMonthYear(request.Month, request.Year, startMonth);
        var filename = $"bang-hach-toan-chi-phi-vat-tu-dai-ky-{resolvedMonth:D2}-{resolvedYear}.xlsx";

        var actualOutput = acceptanceReport.ProductionOutput?.ProductionMeters ?? 0;
        var standardOutput = acceptanceReport.ProductionOutput?.StandardProductionMeters ?? 0;

        if (request.ProcessGroupId.HasValue)
        {
            var groupItem = flattenedItems.FirstOrDefault();
            if (groupItem != null)
            {
                actualOutput = groupItem.ActualOutput;
                standardOutput = groupItem.StandardOutput;
            }
        }

        var fileBytes = BuildWorkbook(flattenedItems, resolvedMonth, resolvedYear, actualOutput, standardOutput);
        return new ExportLongTermMaterialCostExcelResponse(fileBytes, filename);
    }

    private static List<DetailLongTermTrackingItemDto> FlattenItems(GetDetailLongTermTrackingResponseDto detail)
    {
        var results = new List<DetailLongTermTrackingItemDto>();
        var seenLogIds = new HashSet<Guid>();

        foreach (var group in detail.ProcessGroups)
        {
            foreach (var item in group.Items)
            {
                var mergedItem = item with
                {
                    ProcessGroupId = item.ProcessGroupId ?? group.ProcessGroupId,
                    ProcessGroupCode = string.IsNullOrWhiteSpace(item.ProcessGroupCode) ? group.ProcessGroupCode : item.ProcessGroupCode,
                    ProcessGroupName = string.IsNullOrWhiteSpace(item.ProcessGroupName) ? group.ProcessGroupName : item.ProcessGroupName
                };

                results.Add(mergedItem);
                seenLogIds.Add(item.Id);
            }
        }

        foreach (var item in detail.Items)
        {
            if (seenLogIds.Add(item.Id))
            {
                results.Add(item);
            }
        }

        return results
            .OrderBy(x => x.ProcessGroupCode)
            .ThenBy(x => x.MaterialCode ?? x.PartCode)
            .ToList();
    }

    private static (int month, int year) ResolveMonthYear(string? month, string? year, DateOnly fallbackDate)
    {
        var resolvedMonth = fallbackDate.Month;
        var resolvedYear = fallbackDate.Year;

        if (!string.IsNullOrWhiteSpace(month))
        {
            if (!int.TryParse(month, out resolvedMonth) || resolvedMonth is < 1 or > 12)
            {
                throw new BadRequestException("Invalid month. Expected MM format.");
            }
        }

        if (!string.IsNullOrWhiteSpace(year))
        {
            if (!int.TryParse(year, out resolvedYear) || resolvedYear < 1)
            {
                throw new BadRequestException("Invalid year. Expected YYYY format.");
            }
        }

        return (resolvedMonth, resolvedYear);
    }

    private static byte[] BuildWorkbook(
        IReadOnlyList<DetailLongTermTrackingItemDto> items,
        int month,
        int year,
        double actualOutput,
        double standardOutput)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bảng hạch toán");

        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 12;

        var lastCol = 17;
        ConfigureColumnWidths(worksheet);

        worksheet.Range(1, 1, 1, 9).Merge();
        worksheet.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(2, 1, 2, 9).Merge();
        worksheet.Cell(2, 1).Value = "CÔNG TRƯỜNG KHAI THÁC 1";
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(3, 1, 3, lastCol).Merge();
        worksheet.Cell(3, 1).Value = "BẢNG HẠCH TOÁN CHI PHÍ VẬT TƯ DÀI KỲ";
        worksheet.Cell(3, 1).Style.Font.Bold = true;
        worksheet.Cell(3, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(4, 1, 4, lastCol).Merge();
        worksheet.Cell(4, 1).Value = $"Tháng {month} năm {year}";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Cell(5, 14).Value = "Qkh:";
        worksheet.Cell(5, 15).Value = actualOutput;
        worksheet.Cell(5, 16).Value = "Tấn";
        worksheet.Cell(6, 14).Value = "Qđm:";
        worksheet.Cell(6, 15).Value = standardOutput;
        worksheet.Cell(6, 16).Value = "Tấn";
        worksheet.Range(7, 14, 7, 16).Merge();
        worksheet.Cell(7, 14).Value = "Bảng số: 03";

        worksheet.Range(5, 14, 7, 16).Style.Font.Bold = true;
        worksheet.Range(5, 14, 7, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(5, 15).Style.NumberFormat.Format = "#,##0.00";
        worksheet.Cell(6, 15).Style.NumberFormat.Format = "#,##0.00";

        var headerTopRow = 9;
        var headerSubRow = 10;
        var formulaRow = 11;
        var totalRow = 12;
        var firstDataRow = 13;

        WriteMainHeader(worksheet, headerTopRow, headerSubRow);
        WriteFormulaHeader(worksheet, formulaRow);
        WriteTotalRow(worksheet, totalRow, items);
        WriteDataRows(worksheet, firstDataRow, items);

        var lastDataRow = GetLastDataRow(firstDataRow, items);
        var tableEndRow = Math.Max(lastDataRow, totalRow);

        var tableRange = worksheet.Range(headerTopRow, 1, tableEndRow, lastCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        tableRange.Style.Alignment.WrapText = true;

        var footerDateRow = tableEndRow + 2;
        worksheet.Range(footerDateRow, 1, footerDateRow, lastCol).Merge();
        var now = DateTime.Now;
        worksheet.Cell(footerDateRow, 1).Value = $"Hà Lầm, ngày {now.Day} tháng {now.Month} năm {now.Year}";
        worksheet.Cell(footerDateRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        worksheet.Cell(footerDateRow, 1).Style.Font.Italic = true;

        var footerTitleRow1 = footerDateRow + 2;
        var footerTitleRow2 = footerDateRow + 3;
        worksheet.Range(footerTitleRow1, 1, footerTitleRow1, 5).Merge();
        worksheet.Range(footerTitleRow1, 6, footerTitleRow1, 11).Merge();
        worksheet.Range(footerTitleRow1, 12, footerTitleRow1, 17).Merge();
        worksheet.Range(footerTitleRow2, 6, footerTitleRow2, 11).Merge();
        worksheet.Range(footerTitleRow2, 12, footerTitleRow2, 17).Merge();

        worksheet.Cell(footerTitleRow1, 1).Value = "NGƯỜI LẬP";
        worksheet.Cell(footerTitleRow1, 6).Value = "ĐẠI DIỆN BÊN NHẬN KHOÁN";
        worksheet.Cell(footerTitleRow1, 12).Value = "ĐẠI DIỆN BÊN GIAO KHOÁN";
        worksheet.Cell(footerTitleRow2, 6).Value = "QUẢN ĐỐC";
        worksheet.Cell(footerTitleRow2, 12).Value = "PHÒNG KẾ HOẠCH";

        worksheet.Range(footerTitleRow1, 1, footerTitleRow2, 17).Style.Font.Bold = true;
        worksheet.Range(footerTitleRow1, 1, footerTitleRow2, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ConfigureColumnWidths(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Width = 6;
        worksheet.Column(2).Width = 28;
        worksheet.Column(3).Width = 8;
        worksheet.Column(4).Width = 16;
        worksheet.Column(5).Width = 10;
        worksheet.Column(6).Width = 12;
        worksheet.Column(7).Width = 14;
        worksheet.Column(8).Width = 16;
        worksheet.Column(9).Width = 14;
        worksheet.Column(10).Width = 8;
        worksheet.Column(11).Width = 8;
        worksheet.Column(12).Width = 8;
        worksheet.Column(13).Width = 16;
        worksheet.Column(14).Width = 8;
        worksheet.Column(15).Width = 16;
        worksheet.Column(16).Width = 16;
        worksheet.Column(17).Width = 12;
    }

    private static void WriteMainHeader(IXLWorksheet worksheet, int topRow, int subRow)
    {
        worksheet.Range(topRow, 1, subRow, 1).Merge().Value = "STT";
        worksheet.Range(topRow, 2, subRow, 2).Merge().Value = "DANH MỤC VẬT TƯ";
        worksheet.Range(topRow, 3, subRow, 3).Merge().Value = "ĐVT";
        worksheet.Range(topRow, 4, subRow, 4).Merge().Value = "GIÁ TRỊ CHỜ HẠCH TOÁN ĐẦU KỲ (Đồng)";
        worksheet.Range(topRow, 5, topRow, 7).Merge().Value = "GIÁ TRỊ PHÁT SINH TRONG KỲ";
        worksheet.Cell(subRow, 5).Value = "SỐ LƯỢNG";
        worksheet.Cell(subRow, 6).Value = "ĐƠN GIÁ";
        worksheet.Cell(subRow, 7).Value = "THÀNH TIỀN";
        worksheet.Range(topRow, 8, subRow, 8).Merge().Value = "TỔNG GIÁ TRỊ CẦN HẠCH TOÁN (Đồng)";
        worksheet.Range(topRow, 9, subRow, 9).Merge().Value = "NGUYÊN GIÁ (đồng)";
        worksheet.Range(topRow, 10, subRow, 10).Merge().Value = "THỜI GIAN SỬ DỤNG (Ti)";
        worksheet.Range(topRow, 11, subRow, 11).Merge().Value = "THỜI GIAN ĐÃ PHÂN BỔ";
        worksheet.Range(topRow, 12, subRow, 12).Merge().Value = "THỜI GIAN CÒN LẠI";
        worksheet.Range(topRow, 13, subRow, 13).Merge().Value = "GIÁ TRỊ CẦN HẠCH TOÁN THEO ĐỊNH MỨC (Đồng)";
        worksheet.Range(topRow, 14, subRow, 14).Merge().Value = "TỶ LỆ PHÂN BỔ";
        worksheet.Range(topRow, 15, subRow, 15).Merge().Value = "GIÁ TRỊ DÀI KỲ HẠCH TOÁN KỲ NÀY (Đồng)";
        worksheet.Range(topRow, 16, subRow, 16).Merge().Value = "GIÁ TRỊ CUỐI KỲ CHỜ HẠCH TOÁN KỲ SAU (Đồng)";
        worksheet.Range(topRow, 17, subRow, 17).Merge().Value = "GHI CHÚ";

        var headerRange = worksheet.Range(topRow, 1, subRow, 17);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e6e6e6");
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void WriteFormulaHeader(IXLWorksheet worksheet, int row)
    {
        var formulas = new Dictionary<int, string>
        {
            [4] = "(1)",
            [5] = "(2)",
            [6] = "(3)",
            [7] = "(4=2*3)",
            [8] = "(5=1+4)",
            [9] = "(6)",
            [10] = "(7)",
            [11] = "(8)",
            [12] = "(9)",
            [13] = "(10=(6)/(7)*Qkh/Qđm)",
            [14] = "(11)",
            [15] = "(12=10*11)",
            [16] = "(13=5-12)"
        };

        for (var col = 1; col <= 17; col++)
        {
            worksheet.Cell(row, col).Value = formulas.TryGetValue(col, out var value) ? value : string.Empty;
        }

        var formulaRange = worksheet.Range(row, 1, row, 17);
        formulaRange.Style.Font.Bold = true;
        formulaRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e6e6e6");
        formulaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteTotalRow(IXLWorksheet worksheet, int row, IReadOnlyList<DetailLongTermTrackingItemDto> items)
    {
        worksheet.Cell(row, 2).Value = "TỔNG";
        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        worksheet.Cell(row, 4).Value = items.Sum(x => x.PendingValueStartPeriod);
        worksheet.Cell(row, 7).Value = items.Sum(x => x.TotalAmount);
        worksheet.Cell(row, 8).Value = items.Sum(x => x.TotalValueToAccount);
        worksheet.Cell(row, 9).Value = items.Sum(x => x.OriginAmount);
        worksheet.Cell(row, 13).Value = items.Sum(x => x.ValueByStandard);
        worksheet.Cell(row, 15).Value = items.Sum(x => x.AccountedValueThisPeriod);
        worksheet.Cell(row, 16).Value = items.Sum(x => x.PendingValueEndPeriod);

        ApplyNumberFormatsForMonetaryColumns(worksheet, row);

        var totalRange = worksheet.Range(row, 1, row, 17);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f7f7f7");
    }

    private static void WriteDataRows(IXLWorksheet worksheet, int firstDataRow, IReadOnlyList<DetailLongTermTrackingItemDto> items)
    {
        var groupedItems = items
            .GroupBy(x => new
            {
                x.ProcessGroupId,
                x.ProcessGroupCode,
                x.ProcessGroupName
            })
            .OrderBy(x => x.Key.ProcessGroupCode)
            .ThenBy(x => x.Key.ProcessGroupName)
            .ToList();

        var row = firstDataRow;
        var sectionIndex = 1;

        foreach (var group in groupedItems)
        {
            worksheet.Cell(row, 1).Value = sectionIndex;
            worksheet.Cell(row, 2).Value = string.IsNullOrWhiteSpace(group.Key.ProcessGroupName)
                ? "Vật tư"
                : group.Key.ProcessGroupName;
            worksheet.Range(row, 1, row, 17).Style.Font.Bold = true;
            worksheet.Range(row, 1, row, 17).Style.Fill.BackgroundColor = XLColor.FromHtml("#f7f7f7");
            row++;

            foreach (var item in group
                .OrderBy(x => x.MaterialCode ?? x.PartCode)
                .ThenBy(x => x.MaterialName ?? x.PartName))
            {
                worksheet.Cell(row, 2).Value = item.MaterialName ?? item.PartName ?? string.Empty;
                worksheet.Cell(row, 3).Value = item.UnitOfMeasureName ?? string.Empty;
                worksheet.Cell(row, 4).Value = item.PendingValueStartPeriod;
                worksheet.Cell(row, 5).Value = item.IssuedQuantity;
                worksheet.Cell(row, 6).Value = item.UnitPrice;
                worksheet.Cell(row, 7).Value = item.TotalAmount;
                worksheet.Cell(row, 8).Value = item.TotalValueToAccount;
                worksheet.Cell(row, 9).Value = item.OriginAmount;
                worksheet.Cell(row, 10).Value = item.UsageTime;
                worksheet.Cell(row, 11).Value = item.AllocatedTime;
                worksheet.Cell(row, 12).Value = item.RemainingTime;
                worksheet.Cell(row, 13).Value = item.ValueByStandard;
                worksheet.Cell(row, 14).Value = item.AllocationRatio;
                worksheet.Cell(row, 15).Value = item.AccountedValueThisPeriod;
                worksheet.Cell(row, 16).Value = item.PendingValueEndPeriod;
                worksheet.Cell(row, 17).Value = item.Note ?? string.Empty;

                ApplyNumberFormatsForMonetaryColumns(worksheet, row);
                worksheet.Cell(row, 14).Style.NumberFormat.Format = "0.00";
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "0.##";
                worksheet.Cell(row, 10).Style.NumberFormat.Format = "0.##";
                worksheet.Cell(row, 11).Style.NumberFormat.Format = "0.##";
                worksheet.Cell(row, 12).Style.NumberFormat.Format = "0.##";
                row++;
            }

            sectionIndex++;
        }

        if (row > firstDataRow)
        {
            var endRow = row - 1;
            worksheet.Range(firstDataRow, 1, endRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Range(firstDataRow, 2, endRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            worksheet.Range(firstDataRow, 4, endRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            worksheet.Range(firstDataRow, 17, endRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        }
    }

    private static int GetLastDataRow(int firstDataRow, IReadOnlyList<DetailLongTermTrackingItemDto> items)
    {
        if (items.Count == 0)
        {
            return firstDataRow;
        }

        var groupCount = items
            .Select(x => new
            {
                x.ProcessGroupId,
                x.ProcessGroupCode,
                x.ProcessGroupName
            })
            .Distinct()
            .Count();

        return firstDataRow + items.Count + groupCount - 1;
    }

    private static void ApplyNumberFormatsForMonetaryColumns(IXLWorksheet worksheet, int row)
    {
        worksheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 13).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 15).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 16).Style.NumberFormat.Format = "#,##0";
    }
}
