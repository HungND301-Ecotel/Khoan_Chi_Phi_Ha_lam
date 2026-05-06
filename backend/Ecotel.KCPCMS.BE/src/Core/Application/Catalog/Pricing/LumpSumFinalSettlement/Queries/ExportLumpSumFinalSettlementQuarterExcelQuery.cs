using Application.Common.Exceptions;
using Application.Dto.Catalog.LumpSumFinalSettlement;
using ClosedXML.Excel;
using MediatR;

namespace Application.Catalog.Pricing.LumpSumFinalSettlement.Queries;

public record ExportLumpSumFinalSettlementQuarterExcelQuery(
    string Quarter,
    string Year,
    string? ProcessGroupId,
    string? DepartmentId,
    string? Search) : IRequest<ExportLumpSumFinalSettlementQuarterExcelResponse>;

public record ExportLumpSumFinalSettlementQuarterExcelResponse(byte[] FileBytes, string FileName);

public class ExportLumpSumFinalSettlementQuarterExcelQueryHandler(IMediator mediator)
    : IRequestHandler<ExportLumpSumFinalSettlementQuarterExcelQuery, ExportLumpSumFinalSettlementQuarterExcelResponse>
{
    private const string SheetName = "Bang quyet toan";
    private const int TotalColumns = 12;
    private const int HeaderTopRow = 7;
    private const int HeaderBottomRow = 8;

    public async Task<ExportLumpSumFinalSettlementQuarterExcelResponse> Handle(
        ExportLumpSumFinalSettlementQuarterExcelQuery request,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(request.Quarter, out var quarter) || quarter is < 1 or > 4)
        {
            throw new BadRequestException("Invalid quarter");
        }

        if (!int.TryParse(request.Year, out var year) || year < 1)
        {
            throw new BadRequestException("Invalid year");
        }

        var response = await mediator.Send(
            new GetLumpSumFinalSettlementQuarterListQuery(
                request.Quarter,
                request.Year,
                request.ProcessGroupId,
                request.DepartmentId),
            cancellationToken);

        var groupedRows = GroupByProcessGroup(response.Items);
        var reportRows = BuildReportRows(
            groupedRows,
            response.RevenuesByMonth,
            response.TransferredCosts,
            response.CustomCosts,
            response.CoalExcavationActualQuantity,
            response.CoalCrosscutActualQuantity,
            response.MeterExcavationActualQuantity,
            response.MeterCrosscutActualQuantity,
            response.AcceptedSavingQuarter,
            response.SavingsValue,
            quarter,
            year);

        var filteredRows = ApplySearch(reportRows, request.Search);
        var fileBytes = BuildWorkbook(filteredRows, quarter, year);
        var fileName = $"bao-cao-quyet-toan-quy-{quarter}-nam-{year}.xlsx";

        return new ExportLumpSumFinalSettlementQuarterExcelResponse(fileBytes, fileName);
    }

    private static List<ExportRow> GroupByProcessGroup(IReadOnlyCollection<LumpSumFinalSettlementDto> items)
    {
        var groups = new List<(string Key, List<LumpSumFinalSettlementDto> Items)>();

        foreach (var item in items)
        {
            var key = item.ProcessGroupId != Guid.Empty
                ? item.ProcessGroupId.ToString()
                : $"{item.ProcessGroupCode}|{item.ProcessGroupName}";

            var existingGroup = groups.FirstOrDefault(x => x.Key == key);
            if (existingGroup.Items != null)
            {
                existingGroup.Items.Add(item);
                continue;
            }

            groups.Add((key, [item]));
        }

        var result = new List<ExportRow>();
        var stt = 1;

        foreach (var (_, groupItems) in groups)
        {
            var first = groupItems[0];
            var code = first.ProcessGroupCode?.Trim() ?? string.Empty;
            var name = first.ProcessGroupName?.Trim() ?? string.Empty;
            var groupTitle = string.Join(" - ", new[] { code, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
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
                MaterialsUnitPrice = null,
                MaterialsTotalAmount = groupItems.Sum(x => x.Materials?.TotalAmount ?? 0),
                MaintainsUnitPrice = null,
                MaintainsTotalAmount = groupItems.Sum(x => x.Maintains?.TotalAmount ?? 0),
                ElectricitiesUnitPrice = null,
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

    private static List<ExportRow> BuildReportRows(
        IReadOnlyList<ExportRow> groupedRows,
        IReadOnlyList<LumpSumQuarterRevenueByMonthDto> revenuesByMonth,
        IReadOnlyList<LumpSumQuarterTransferredCostDto> transferredCosts,
        IReadOnlyList<LumpSumQuarterCustomCostDto> customCosts,
        double coalExcavationActualQuantity,
        double coalCrosscutActualQuantity,
        double meterExcavationActualQuantity,
        double meterCrosscutActualQuantity,
        double acceptedSavingQuarterFromResponse,
        double savingsValue,
        int quarter,
        int year)
    {
        var quarterRoman = ToRomanQuarter(quarter);
        var months = QuarterToMonthRange(quarter);
        var revenueByMonthMap = revenuesByMonth.ToDictionary(x => x.Month, x => x);

        var revenueRows = months.Select(monthNumber =>
        {
            revenueByMonthMap.TryGetValue(monthNumber, out var value);
            return new SummaryAmounts(
                monthNumber,
                value?.Materials?.TotalAmount ?? 0,
                value?.Maintains?.TotalAmount ?? 0,
                value?.Electricities?.TotalAmount ?? 0,
                value?.TotalAmount ?? 0);
        }).ToList();

        var revenueQuarter = SummaryAmounts.Sum(revenueRows);

        var transferredByMonth = transferredCosts.ToDictionary(
            x => x.Month,
            x => new SummaryAmounts(
                x.Month,
                x.Materials?.TotalAmount ?? 0,
                x.Maintains?.TotalAmount ?? 0,
                x.Electricities?.TotalAmount ?? 0,
                x.TotalAmount));

        var customCostRowsByMonth = new Dictionary<int, List<ExportRow>>();
        var customCostTotalsByMonth = new Dictionary<int, SummaryAmounts>();

        foreach (var item in customCosts)
        {
            var month = item.Month;
            if (month <= 0)
            {
                continue;
            }

            var row = BuildCustomCostRow(item);
            if (!customCostRowsByMonth.TryGetValue(month, out var rows))
            {
                rows = [];
                customCostRowsByMonth[month] = rows;
            }

            rows.Add(row);

            customCostTotalsByMonth.TryGetValue(month, out var currentTotal);
            customCostTotalsByMonth[month] = new SummaryAmounts(
                month,
                (currentTotal?.Materials ?? 0) + row.MaterialsTotalAmount,
                (currentTotal?.Maintains ?? 0) + row.MaintainsTotalAmount,
                (currentTotal?.Electricities ?? 0) + row.ElectricitiesTotalAmount,
                (currentTotal?.Total ?? 0) + row.TotalAmount);
        }

        var costRows = months.Select(month =>
        {
            transferredByMonth.TryGetValue(month, out var monthTransferred);
            customCostTotalsByMonth.TryGetValue(month, out var monthCustom);
            return new SummaryAmounts(
                month,
                (monthTransferred?.Materials ?? 0) + (monthCustom?.Materials ?? 0),
                (monthTransferred?.Maintains ?? 0) + (monthCustom?.Maintains ?? 0),
                (monthTransferred?.Electricities ?? 0) + (monthCustom?.Electricities ?? 0),
                (monthTransferred?.Total ?? 0) + (monthCustom?.Total ?? 0));
        }).ToList();

        var costQuarter = SummaryAmounts.Sum(costRows);

        var savingRows = months.Select((month, index) =>
        {
            var revenue = revenueRows[index];
            var cost = costRows[index];
            return new SummaryAmounts(
                month,
                revenue.Materials - cost.Materials,
                revenue.Maintains - cost.Maintains,
                revenue.Electricities - cost.Electricities,
                revenue.Total - cost.Total);
        }).ToList();

        var savingQuarter = SummaryAmounts.Sum(savingRows);
        var acceptedSavingQuarter = acceptedSavingQuarterFromResponse;
        if (Math.Abs(acceptedSavingQuarter) < double.Epsilon)
        {
            acceptedSavingQuarter =
                savingQuarter.Materials +
                savingQuarter.Maintains +
                savingQuarter.Electricities;
        }

        var savingAddedToIncomeByMonth = months
            .Select((_, index) =>
            {
                var acceptedSavingMonth =
                    savingRows[index].Materials +
                    savingRows[index].Maintains +
                    savingRows[index].Electricities;
                return acceptedSavingMonth * savingsValue;
            })
            .ToList();
        var savingAddedToIncomeQuarter = savingAddedToIncomeByMonth.Sum();

        var specialRows = new List<ExportRow>
        {
            new()
            {
                SttLabel = "1",
                ProductName = "Than dao lo",
                UnitOfMeasureName = "Tan",
                PlannedQuantity = null,
                ActualQuantity = coalExcavationActualQuantity,
                IsBold = true,
                ExcludeFromSummary = true
            },
            new()
            {
                SttLabel = "2",
                ProductName = "Than xen lo",
                UnitOfMeasureName = "Tan",
                PlannedQuantity = null,
                ActualQuantity = coalCrosscutActualQuantity,
                IsBold = true,
                ExcludeFromSummary = true
            },
            new()
            {
                SttLabel = "3",
                ProductName = "Met lo dao",
                UnitOfMeasureName = "m",
                PlannedQuantity = null,
                ActualQuantity = meterExcavationActualQuantity,
                IsBold = true,
                ExcludeFromSummary = true
            },
            new()
            {
                SttLabel = "4",
                ProductName = "Met xen lo",
                UnitOfMeasureName = "m",
                PlannedQuantity = null,
                ActualQuantity = meterCrosscutActualQuantity,
                IsBold = true,
                ExcludeFromSummary = true
            }
        };

        var defaultRows = new List<ExportRow>
        {
            MakeZeroRow($"Doanh thu quy {quarterRoman}/{year}", sttLabel: "I", isBold: true,
                materialsTotalAmount: revenueQuarter.Materials,
                maintainsTotalAmount: revenueQuarter.Maintains,
                electricitiesTotalAmount: revenueQuarter.Electricities,
                totalAmount: revenueQuarter.Total,
                hidePlanActual: true, hideUnitPrice: true)
        };

        defaultRows.AddRange(months.Select((monthNumber, index) =>
            MakeZeroRow($"Thang {monthNumber}/{year}", sttLabel: $"I.{index + 1}",
                unitOfMeasureName: "Dong",
                materialsTotalAmount: revenueRows[index].Materials,
                maintainsTotalAmount: revenueRows[index].Maintains,
                electricitiesTotalAmount: revenueRows[index].Electricities,
                totalAmount: revenueRows[index].Total,
                hidePlanActual: true, hideUnitPrice: true)));

        defaultRows.Add(MakeZeroRow($"Chi phi quy {quarterRoman}/{year}", sttLabel: "II", isBold: true,
            materialsTotalAmount: costQuarter.Materials,
            maintainsTotalAmount: costQuarter.Maintains,
            electricitiesTotalAmount: costQuarter.Electricities,
            totalAmount: costQuarter.Total,
            hidePlanActual: true, hideUnitPrice: true));

        foreach (var (monthNumber, index) in months.Select((month, idx) => (month, idx)))
        {
            transferredByMonth.TryGetValue(monthNumber, out var monthTransferred);
            var monthCost = costRows[index];

            defaultRows.Add(MakeZeroRow($"Thang {monthNumber}/{year}", sttLabel: $"II.{index + 1}",
                unitOfMeasureName: "Dong",
                materialsTotalAmount: monthCost.Materials,
                maintainsTotalAmount: monthCost.Maintains,
                electricitiesTotalAmount: monthCost.Electricities,
                totalAmount: monthCost.Total,
                hidePlanActual: true, hideUnitPrice: true));

            defaultRows.Add(MakeZeroRow($"Chi phi ket chuyen T{monthNumber}/{year}", sttLabel: "-",
                month: monthNumber,
                materialsTotalAmount: monthTransferred?.Materials ?? 0,
                maintainsTotalAmount: monthTransferred?.Maintains ?? 0,
                electricitiesTotalAmount: monthTransferred?.Electricities ?? 0,
                totalAmount: monthTransferred?.Total ?? 0,
                hidePlanActual: true, hideUnitPrice: true,
                isTransferredDefaultRow: true));

            if (customCostRowsByMonth.TryGetValue(monthNumber, out var customRows))
            {
                defaultRows.AddRange(customRows);
            }
        }

        defaultRows.Add(MakeZeroRow($"Gia tri tiet kiem, boi chi quy {quarterRoman}/{year}", sttLabel: "III", isBold: true,
            materialsTotalAmount: savingQuarter.Materials,
            maintainsTotalAmount: savingQuarter.Maintains,
            electricitiesTotalAmount: savingQuarter.Electricities,
            totalAmount: savingQuarter.Total,
            hidePlanActual: true, hideUnitPrice: true));

        defaultRows.AddRange(months.Select((monthNumber, index) =>
            MakeZeroRow($"Thang {monthNumber}/{year}", sttLabel: $"III.{index + 1}",
                unitOfMeasureName: "Dong",
                materialsTotalAmount: savingRows[index].Materials,
                maintainsTotalAmount: savingRows[index].Maintains,
                electricitiesTotalAmount: savingRows[index].Electricities,
                totalAmount: savingRows[index].Total,
                hidePlanActual: true, hideUnitPrice: true)));

        defaultRows.Add(MakeZeroRow($"Tong gia tri tiet kiem duoc chap nhan quy {quarterRoman}/{year}", sttLabel: "*", isBold: true,
            unitOfMeasureName: "Dong", hidePlanActual: true, hideUnitPrice: true,
            isMergedValueRow: true, mergedValue: acceptedSavingQuarter));
        defaultRows.Add(MakeZeroRow($"Gia tri tiet kiem duoc cong vao thu nhap quy {quarterRoman}/{year}", sttLabel: "*", isBold: true,
            unitOfMeasureName: "Dong", hidePlanActual: true, hideUnitPrice: true,
            isMergedValueRow: true, mergedValue: savingAddedToIncomeQuarter));
        defaultRows.AddRange(months.Select((monthNumber, index) =>
            MakeZeroRow($"Gia tri tiet kiem da cong vao thu nhap thang {monthNumber}/{year}", sttLabel: "*",
                unitOfMeasureName: "Dong", hidePlanActual: true, hideUnitPrice: true,
                isMergedValueRow: true,
                mergedValue: savingAddedToIncomeByMonth.Count > index
                    ? savingAddedToIncomeByMonth[index]
                    : 0)));

        return [.. specialRows, .. groupedRows, .. defaultRows];
    }

    private static List<ExportRow> ApplySearch(IReadOnlyList<ExportRow> rows, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return [.. rows];
        }

        var query = search.Trim().ToLowerInvariant();
        return rows
            .Where(row =>
            {
                var keywords = string.Join(
                    ' ',
                    new[] { row.ProductCode, row.ProductName, row.UnitOfMeasureName }
                        .Where(x => !string.IsNullOrWhiteSpace(x)))
                    .ToLowerInvariant();

                return keywords.Contains(query);
            })
            .ToList();
    }

    private static ExportRow BuildCustomCostRow(LumpSumQuarterCustomCostDto item)
    {
        var quantity = item.ActualQuantity;
        var materialUnit = item.MaterialUnitPrice;
        var maintainUnit = item.MaintainUnitPrice;
        var electricityUnit = item.ElectricityUnitPrice;
        var materialTotal = quantity * materialUnit;
        var maintainTotal = quantity * maintainUnit;
        var electricityTotal = quantity * electricityUnit;

        return new ExportRow
        {
            SttLabel = "-",
            Month = item.Month,
            ProductName = item.CustomName,
            UnitOfMeasureName = "Dong",
            ActualQuantity = quantity,
            MaterialsUnitPrice = materialUnit,
            MaterialsTotalAmount = materialTotal,
            MaintainsUnitPrice = maintainUnit,
            MaintainsTotalAmount = maintainTotal,
            ElectricitiesUnitPrice = electricityUnit,
            ElectricitiesTotalAmount = electricityTotal,
            TotalAmount = materialTotal + maintainTotal + electricityTotal,
            ExcludeFromSummary = true
        };
    }

    private static ExportRow MakeZeroRow(
        string productName,
        string? sttLabel = null,
        bool isBold = false,
        int? month = null,
        string unitOfMeasureName = "",
        double materialsTotalAmount = 0,
        double maintainsTotalAmount = 0,
        double electricitiesTotalAmount = 0,
        double totalAmount = 0,
        bool hidePlanActual = false,
        bool hideUnitPrice = false,
        bool isMergedValueRow = false,
        bool isTransferredDefaultRow = false,
        double? mergedValue = null)
    {
        return new ExportRow
        {
            SttLabel = sttLabel,
            IsBold = isBold,
            ExcludeFromSummary = true,
            IsMergedValueRow = isMergedValueRow,
            IsTransferredDefaultRow = isTransferredDefaultRow,
            Month = month,
            ProductName = productName,
            UnitOfMeasureName = unitOfMeasureName,
            PlannedQuantity = hidePlanActual ? null : 0,
            ActualQuantity = hidePlanActual ? null : 0,
            MaterialsUnitPrice = hideUnitPrice ? null : 0,
            MaterialsTotalAmount = materialsTotalAmount,
            MaintainsUnitPrice = hideUnitPrice ? null : 0,
            MaintainsTotalAmount = maintainsTotalAmount,
            ElectricitiesUnitPrice = hideUnitPrice ? null : 0,
            ElectricitiesTotalAmount = electricitiesTotalAmount,
            TotalAmount = totalAmount,
            MergedValue = mergedValue
        };
    }

    private static byte[] BuildWorkbook(IReadOnlyList<ExportRow> rows, int quarter, int year)
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
        WriteReportHeader(worksheet, quarter, year);
        WriteHeader(worksheet, HeaderTopRow);
        WriteSummaryRow(worksheet, rows);

        var currentRow = HeaderBottomRow + 2;
        foreach (var row in rows)
        {
            if (row.IsMergedValueRow)
            {
                WriteMergedValueRow(worksheet, currentRow, row);
            }
            else
            {
                WriteDataRow(worksheet, currentRow, row);
            }
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
        WriteReportFooter(worksheet, currentRow + 4, quarter, year);

        var tableRange = worksheet.Range(HeaderTopRow, 1, Math.Max(currentRow - 1, HeaderTopRow), TotalColumns);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        tableRange.Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteReportHeader(IXLWorksheet worksheet, int quarter, int year)
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
        worksheet.Cell(4, 1).Value = "BANG QUYET TOAN";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Font.FontSize = 16;
        worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(5, 1, 5, TotalColumns).Merge();
        worksheet.Cell(5, 1).Value = $"QUY {ToRomanQuarter(quarter)} NAM {year}";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 1).Style.Font.FontSize = 14;
        worksheet.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteHeader(IXLWorksheet worksheet, int row)
    {
        worksheet.Range(row, 1, HeaderBottomRow, 1).Merge().Value = "STT";
        worksheet.Range(row, 2, HeaderBottomRow, 2).Merge().Value = "SAN PHAM";
        worksheet.Range(row, 3, HeaderBottomRow, 3).Merge().Value = "DVT";
        worksheet.Range(row, 4, HeaderBottomRow, 4).Merge().Value = "KH";
        worksheet.Range(row, 5, HeaderBottomRow, 5).Merge().Value = "TH";

        worksheet.Range(row, 6, row, 7).Merge().Value = "VAT LIEU";
        worksheet.Range(row, 8, row, 9).Merge().Value = "SUA CHUA THUONG XUYEN";
        worksheet.Range(row, 10, row, 11).Merge().Value = "DONG LUC (DIEN NANG)";
        worksheet.Range(row, 12, HeaderBottomRow, 12).Merge().Value = "TONG";

        worksheet.Cell(HeaderBottomRow, 6).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 7).Value = "THANH TIEN";
        worksheet.Cell(HeaderBottomRow, 8).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 9).Value = "THANH TIEN";
        worksheet.Cell(HeaderBottomRow, 10).Value = "DON GIA";
        worksheet.Cell(HeaderBottomRow, 11).Value = "THANH TIEN";

        var headerRange = worksheet.Range(row, 1, HeaderBottomRow, TotalColumns);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F3F4F6");
    }

    private static void WriteSummaryRow(IXLWorksheet worksheet, IReadOnlyList<ExportRow> rows)
    {
        var summaryRow = HeaderBottomRow + 1;
        var visibleRows = rows.Where(x => !x.IsProcessGroupRow && !x.ExcludeFromSummary).ToList();

        worksheet.Row(summaryRow).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");
        worksheet.Row(summaryRow).Style.Font.Bold = true;

        SetNumberCell(worksheet.Cell(summaryRow, 4), visibleRows.Sum(x => x.PlannedQuantity ?? 0), true);
        SetNumberCell(worksheet.Cell(summaryRow, 5), visibleRows.Sum(x => x.ActualQuantity ?? 0), true);
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
        SetNumberCell(worksheet.Cell(rowNumber, 4), row.PlannedQuantity, true);
        SetNumberCell(worksheet.Cell(rowNumber, 5), row.ActualQuantity, true);
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

    private static void WriteMergedValueRow(IXLWorksheet worksheet, int rowNumber, ExportRow row)
    {
        worksheet.Cell(rowNumber, 1).Value = row.SttLabel ?? string.Empty;
        worksheet.Cell(rowNumber, 2).Value = row.ProductName ?? string.Empty;
        worksheet.Cell(rowNumber, 3).Value = row.UnitOfMeasureName ?? string.Empty;

        worksheet.Range(rowNumber, 6, rowNumber, 10).Merge();
        SetNumberCell(worksheet.Cell(rowNumber, 6), row.MergedValue ?? 0);
        worksheet.Cell(rowNumber, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

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

    private static void WriteReportFooter(IXLWorksheet worksheet, int startRow, int quarter, int year)
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
        worksheet.Cell(startRow + 5, 1).Value = $"Bieu mau quy {ToRomanQuarter(quarter)}/{year}";
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

    private static void SetNumberCell(IXLCell cell, double? value, bool allowDecimalDisplay = false)
    {
        if (!value.HasValue)
        {
            cell.Value = string.Empty;
            return;
        }

        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = allowDecimalDisplay ? "#,##0.###" : "#,##0";
    }

    private static List<int> QuarterToMonthRange(int quarter)
    {
        var startMonth = (quarter - 1) * 3 + 1;
        return [startMonth, startMonth + 1, startMonth + 2];
    }

    private static string ToRomanQuarter(int quarter)
    {
        return quarter switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => quarter.ToString()
        };
    }

    private sealed record SummaryAmounts(
        int Month,
        double Materials,
        double Maintains,
        double Electricities,
        double Total)
    {
        public static SummaryAmounts Sum(IEnumerable<SummaryAmounts> rows)
        {
            return new SummaryAmounts(
                0,
                rows.Sum(x => x.Materials),
                rows.Sum(x => x.Maintains),
                rows.Sum(x => x.Electricities),
                rows.Sum(x => x.Total));
        }
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
        public bool ExcludeFromSummary { get; init; }
        public bool IsMergedValueRow { get; init; }
        public bool IsTransferredDefaultRow { get; init; }
        public int? Month { get; init; }
        public double? MergedValue { get; init; }
    }
}
