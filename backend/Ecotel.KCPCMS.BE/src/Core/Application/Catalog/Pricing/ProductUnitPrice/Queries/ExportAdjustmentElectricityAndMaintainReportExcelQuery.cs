using Application.Catalog.Pricing.AdjustmentElectricityCost.Queries;
using Application.Catalog.Pricing.AdjustmnetMaintainCost.Queries;
using Application.Catalog.Pricing.PlannedElectricityCost.Queries;
using Application.Catalog.Pricing.PlannedMaintainCost.Queries;
using Application.Common.Exceptions;
using Application.Dto.Catalog.AdjustmentElectricityCost;
using Application.Dto.Catalog.AdjustmentMaintainCost;
using Application.Dto.Catalog.PlannedElectricityCost;
using Application.Dto.Catalog.PlannedMaintainCost;
using Application.Dto.Catalog.ProductUnitPrice;
using ClosedXML.Excel;
using Domain.Common.Enums;
using MediatR;

namespace Application.Catalog.Pricing.ProductUnitPrice.Queries;

public record ExportAdjustmentElectricityAndMaintainReportExcelQuery(
    string? Month,
    string? Year,
    Guid? ProcessGroupId,
    ProductUnitPriceScenarioType ScenarioType = ProductUnitPriceScenarioType.Adjustment) : IRequest<ExportAdjustmentElectricityAndMaintainReportExcelResponse>;

public record ExportAdjustmentElectricityAndMaintainReportExcelResponse(byte[] FileBytes, string FileName);

public class ExportAdjustmentElectricityAndMaintainReportExcelQueryHandler(IMediator mediator)
    : IRequestHandler<ExportAdjustmentElectricityAndMaintainReportExcelQuery, ExportAdjustmentElectricityAndMaintainReportExcelResponse>
{
    private const int KFactorCount = 7;

    public async Task<ExportAdjustmentElectricityAndMaintainReportExcelResponse> Handle(
        ExportAdjustmentElectricityAndMaintainReportExcelQuery request,
        CancellationToken cancellationToken)
    {
        var (resolvedMonth, resolvedYear) = ResolveMonthYear(request.Month, request.Year);

        var products = await mediator.Send(
            new GetAllProductUnitPriceQuery(1, int.MaxValue, string.Empty, true, request.ScenarioType),
            cancellationToken);

        var periodFilteredProducts = products.Data
            .Where(x => IsMonthWithinRange(x.StartMonth, x.EndMonth, resolvedYear, resolvedMonth))
            .Where(x => !request.ProcessGroupId.HasValue || x.ProcessGroupId == request.ProcessGroupId.Value)
            .ToList();

        var reportBlocks = new List<ReportBlock>();

        if (request.ScenarioType == ProductUnitPriceScenarioType.Plan)
        {
            foreach (var product in periodFilteredProducts)
            {
                var detail = await mediator.Send(new GetPlannedProductUnitPriceByIdQuery(product.Id), cancellationToken);

                var periodItems = detail.Outputs
                    .Where(o => IsMonthWithinRange(o.StartMonth, o.EndMonth, resolvedYear, resolvedMonth))
                    .Select(o => new {
                        o.Id,
                        o.StartMonth,
                        o.EndMonth,
                        o.ProductionMeters,
                        o.PlannedMaintainCostId,
                        o.PlannedElectricityCostId
                    })
                    .ToList();

                foreach (var periodItem in periodItems)
                {
                    var maintainCosts = new List<AdjustmentMaintainCostAdjDto>();
                    if (periodItem.PlannedMaintainCostId.HasValue)
                    {
                        var maintain = await TryGetPlannedMaintainCost(periodItem.PlannedMaintainCostId.Value, cancellationToken);
                        if (maintain != null)
                        {
                            maintainCosts = maintain.Costs.Select(c => new AdjustmentMaintainCostAdjDto
                            {
                                MaintainUnitPriceId = c.MaintainUnitPriceId,
                                MaintainUnitPrice = c.MaintainUnitPrice,
                                EquipmentId = c.EquipmentId,
                                EquipmentCode = c.EquipmentCode,
                                EquipmentName = c.EquipmentName,
                                Quantity = c.Quantity,
                                K6AdjustmentFactorValue = c.K6AdjustmentFactorValue,
                                TotalPrice = c.TotalPrice,
                                AdjustmentFactorDescriptions = c.AdjustmentFactorDescriptions
                            }).ToList();
                        }
                    }

                    var electricityCosts = new List<AdjustmentElectricityCostAdjDto>();
                    if (periodItem.PlannedElectricityCostId.HasValue)
                    {
                        var electricity = await TryGetPlannedElectricityCost(periodItem.PlannedElectricityCostId.Value, cancellationToken);
                        if (electricity != null)
                        {
                            electricityCosts = electricity.Costs.Select(c => new AdjustmentElectricityCostAdjDto
                            {
                                ElectricityUnitPriceEquipmentId = c.ElectricityUnitPriceEquipmentId,
                                ElectricityUnitPrice = c.ElectricityUnitPrice,
                                EquipmentId = c.EquipmentId,
                                EquipmentCode = c.EquipmentCode,
                                EquipmentName = c.EquipmentName,
                                Quantity = c.Quantity,
                                TotalPrice = c.TotalPrice,
                                AdjustmentFactorDescriptions = c.AdjustmentFactorDescriptions
                            }).ToList();
                        }
                    }

                    var rows = BuildRows(periodItem.Id, maintainCosts, electricityCosts);
                    if (!rows.Any())
                    {
                        continue;
                    }

                    reportBlocks.Add(new ReportBlock(
                        ProcessGroupLabel: BuildProcessGroupLabelPlanned(detail),
                        ProductName: detail.ProductName,
                        ProductUnitLabel: ResolveProductUnitLabel(detail.ProcessGroupType),
                        ProductionMeters: periodItem.ProductionMeters,
                        Rows: rows));
                }
            }
        }
        else
        {
            foreach (var product in periodFilteredProducts)
            {
                var detail = await mediator.Send(new GetAdjustmentProductUnitPriceByIdQuery(product.Id), cancellationToken);

                var hasOutputs = detail.Outputs is { Count: > 0 };
                var periodItems = hasOutputs
                    ? detail.Outputs
                        .Where(o => IsMonthWithinRange(o.StartMonth, o.EndMonth, resolvedYear, resolvedMonth))
                        .Select(o => new PeriodItem(
                            o.Id,
                            o.StartMonth,
                            o.EndMonth,
                            o.ProductionMeters,
                            null))
                        .ToList()
                    : detail.ProductionOutputs
                        .Where(o => IsMonthWithinRange(o.StartMonth, o.EndMonth, resolvedYear, resolvedMonth))
                        .Select(o => new PeriodItem(
                            o.Id,
                            o.StartMonth,
                            o.EndMonth,
                            o.ProductionMeters,
                            o.StandardProductionMeters))
                        .ToList();

                foreach (var periodItem in periodItems)
                {
                    var maintain = await TryGetMaintainCost(periodItem.OutputId, cancellationToken);
                    var electricity = await TryGetElectricityCost(periodItem.OutputId, cancellationToken);

                    var rows = BuildRows(periodItem.OutputId, maintain?.Costs, electricity?.Costs);
                    if (!rows.Any())
                    {
                        continue;
                    }

                    reportBlocks.Add(new ReportBlock(
                        ProcessGroupLabel: BuildProcessGroupLabel(detail),
                        ProductName: detail.ProductName,
                        ProductUnitLabel: ResolveProductUnitLabel(detail.ProcessGroupType),
                        ProductionMeters: periodItem.ProductionMeters ?? 0,
                        Rows: rows));
                }
            }
        }

        var fileBytes = BuildWorkbook(reportBlocks, resolvedMonth, resolvedYear);
        var fileName = $"bang-tinh-don-gia-sctx-va-dien-nang-thang-{resolvedMonth:D2}-nam-{resolvedYear}.xlsx";
        return new ExportAdjustmentElectricityAndMaintainReportExcelResponse(fileBytes, fileName);
    }

    private async Task<PlannedMaintainCostDetailDto?> TryGetPlannedMaintainCost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new GetPlannedMaintainCostByIdQuery(id), cancellationToken);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task<PlannedElectricityCostDetailDto?> TryGetPlannedElectricityCost(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new GetPlannedElectricityCostByIdQuery(id), cancellationToken);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static string BuildProcessGroupLabelPlanned(PlannedProductUnitPriceDetailDto detail)
    {
        var parts = new[] { detail.ProcessGroupCode, detail.ProcessGroupName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return parts.Count > 0 ? string.Join(" - ", parts) : "Chưa phân nhóm";
    }

    private async Task<AdjustmentMaintainCostDetailDto?> TryGetMaintainCost(Guid outputId, CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new GetAdjustmentMaintainCostByOutputIdQuery(outputId), cancellationToken);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task<AdjustmentElectricityCostDetailDto?> TryGetElectricityCost(Guid outputId, CancellationToken cancellationToken)
    {
        try
        {
            return await mediator.Send(new GetAdjustmentElectricityCostByOutputIdQuery(outputId), cancellationToken);
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private static string BuildProcessGroupLabel(AdjustmentProductUnitPriceDetailDto detail)
    {
        var parts = new[] { detail.ProcessGroupCode, detail.ProcessGroupName }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return parts.Count > 0 ? string.Join(" - ", parts) : "Chưa phân nhóm";
    }

    private static string ResolveProductUnitLabel(ProcessGroupType type)
    {
        return type switch
        {
            ProcessGroupType.DL => "Mét",
            ProcessGroupType.LC => "Tấn",
            _ => string.Empty,
        };
    }

    private static List<ReportRow> BuildRows(
        Guid outputId,
        IList<AdjustmentMaintainCostAdjDto>? maintainCosts,
        IList<AdjustmentElectricityCostAdjDto>? electricityCosts)
    {
        var maintainMap = (maintainCosts ?? [])
            .GroupBy(x => x.EquipmentId)
            .ToDictionary(g => g.Key, g => g.First());

        var electricityMap = (electricityCosts ?? [])
            .GroupBy(x => x.EquipmentId)
            .ToDictionary(g => g.Key, g => g.First());

        var equipmentIds = maintainMap.Keys
            .Concat(electricityMap.Keys)
            .Distinct()
            .ToList();

        var rows = new List<ReportRow>();
        foreach (var equipmentId in equipmentIds)
        {
            maintainMap.TryGetValue(equipmentId, out var maintain);
            electricityMap.TryGetValue(equipmentId, out var electricity);

            var kValues = ExtractMaintainFactors(maintain)
                ?? ExtractElectricityFactors(electricity)
                ?? Enumerable.Repeat(1d, KFactorCount).ToArray();

            rows.Add(new ReportRow(
                Key: $"{outputId}-{equipmentId}",
                AssignmentCodeName: maintain?.EquipmentName ?? electricity?.EquipmentName ?? string.Empty,
                UnitOfMeasureName: "Cái",
                Quantity: (double)(maintain?.Quantity ?? electricity?.Quantity ?? 0),
                KValues: kValues,
                MaintainUnitPrice: maintain?.MaintainUnitPrice,
                MaintainTotalPrice: maintain?.TotalPrice,
                ElectricityUnitPrice: electricity?.ElectricityUnitPrice,
                ElectricityTotalPrice: electricity?.TotalPrice));
        }

        return rows;
    }

    private static double[]? ExtractMaintainFactors(AdjustmentMaintainCostAdjDto? item)
    {
        if (item == null)
        {
            return null;
        }

        var sorted = item.AdjustmentFactorDescriptions
            .OrderBy(x => x.AdjustmentFactorCode)
            .ToList();

        return
        [
            sorted.ElementAtOrDefault(0)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(1)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(2)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(3)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(4)?.EffectiveValue ?? 1,
            item.K6AdjustmentFactorValue,
            sorted.ElementAtOrDefault(5)?.EffectiveValue ?? 1,
        ];
    }

    private static double[]? ExtractElectricityFactors(AdjustmentElectricityCostAdjDto? item)
    {
        if (item == null)
        {
            return null;
        }

        var sorted = item.AdjustmentFactorDescriptions
            .OrderBy(x => x.AdjustmentFactorCode)
            .ToList();

        return
        [
            sorted.ElementAtOrDefault(0)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(1)?.EffectiveValue ?? 1,
            sorted.ElementAtOrDefault(2)?.EffectiveValue ?? 1,
            1,
            1,
            1,
            1,
        ];
    }

    private static bool IsMonthWithinRange(DateOnly? startDate, DateOnly? endDate, int year, int month)
    {
        if (!startDate.HasValue)
        {
            return false;
        }

        var effectiveEnd = endDate ?? startDate;
        if (!effectiveEnd.HasValue)
        {
            return false;
        }

        var startIndex = (startDate.Value.Year * 12) + startDate.Value.Month - 1;
        var endIndex = (effectiveEnd.Value.Year * 12) + effectiveEnd.Value.Month - 1;
        var targetIndex = (year * 12) + month - 1;

        return targetIndex >= startIndex && targetIndex <= endIndex;
    }

    private static (int month, int year) ResolveMonthYear(string? month, string? year)
    {
        var now = DateTime.Now;
        var resolvedMonth = now.Month;
        var resolvedYear = now.Year;

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

    private static byte[] BuildWorkbook(IReadOnlyList<ReportBlock> blocks, int month, int year)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bảng tính SCTX và điện năng");
        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 12;

        ConfigureColumnWidths(worksheet);

        const int totalColumns = 18;
        worksheet.Range(1, 1, 1, totalColumns).Merge();
        worksheet.Cell(1, 1).Value = "BẢNG TÍNH ĐƠN GIÁ SCTX VÀ ĐIỆN NĂNG";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(2, 1, 2, totalColumns).Merge();
        worksheet.Cell(2, 1).Value = $"Tháng {month} năm {year}";
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var headerTopRow = 4;
        var headerBottomRow = 5;
        WriteHeader(worksheet, headerTopRow, headerBottomRow);

        var row = 6;
        var grouped = blocks.GroupBy(x => x.ProcessGroupLabel).OrderBy(x => x.Key).ToList();

        for (var groupIndex = 0; groupIndex < grouped.Count; groupIndex++)
        {
            var group = grouped[groupIndex];
            worksheet.Range(row, 1, row, totalColumns).Merge();
            worksheet.Cell(row, 1).Value = $"{ToRoman(groupIndex + 1)}. {group.Key}";
            worksheet.Cell(row, 1).Style.Font.Bold = true;
            row++;

            var blockIndex = 1;
            foreach (var block in group)
            {
                var startRow = row;
                for (var rowIndex = 0; rowIndex < block.Rows.Count; rowIndex++)
                {
                    var item = block.Rows[rowIndex];
                    var excelRow = row + rowIndex;

                    if (rowIndex == 0)
                    {
                        worksheet.Cell(excelRow, 1).Value = blockIndex;
                        worksheet.Cell(excelRow, 2).Value = block.ProductName;
                        worksheet.Cell(excelRow, 3).Value = block.ProductUnitLabel;
                        worksheet.Cell(excelRow, 4).Value = block.ProductionMeters;
                    }

                    worksheet.Cell(excelRow, 5).Value = item.AssignmentCodeName;
                    worksheet.Cell(excelRow, 6).Value = item.UnitOfMeasureName;
                    worksheet.Cell(excelRow, 7).Value = item.Quantity;

                    for (var k = 0; k < KFactorCount; k++)
                    {
                        worksheet.Cell(excelRow, 8 + k).Value = item.KValues[k];
                    }

                    SetRoundedCell(worksheet.Cell(excelRow, 15), item.MaintainUnitPrice);
                    SetRoundedCell(worksheet.Cell(excelRow, 16), item.MaintainTotalPrice);
                    SetRoundedCell(worksheet.Cell(excelRow, 17), item.ElectricityUnitPrice);
                    SetRoundedCell(worksheet.Cell(excelRow, 18), item.ElectricityTotalPrice);
                }

                var endRow = row + block.Rows.Count - 1;
                if (endRow > startRow)
                {
                    for (var col = 1; col <= 4; col++)
                    {
                        worksheet.Range(startRow, col, endRow, col).Merge();
                        worksheet.Range(startRow, col, endRow, col).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    }
                }

                row += block.Rows.Count;
                blockIndex++;
            }
        }

        if (row == 6)
        {
            worksheet.Range(row, 1, row, totalColumns).Merge();
            worksheet.Cell(row, 1).Value = "Không có dữ liệu";
            worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;
        }

        var tableRange = worksheet.Range(headerTopRow, 1, row - 1, totalColumns);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetRoundedCell(IXLCell cell, double? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        cell.Value = Math.Round(value.Value, 0, MidpointRounding.AwayFromZero);
        cell.Style.NumberFormat.Format = "#,##0";
    }

    private static void ConfigureColumnWidths(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Width = 6;
        worksheet.Column(2).Width = 24;
        worksheet.Column(3).Width = 10;
        worksheet.Column(4).Width = 12;
        worksheet.Column(5).Width = 24;
        worksheet.Column(6).Width = 8;
        worksheet.Column(7).Width = 10;

        for (var col = 8; col <= 14; col++)
        {
            worksheet.Column(col).Width = 7;
        }

        worksheet.Column(15).Width = 14;
        worksheet.Column(16).Width = 14;
        worksheet.Column(17).Width = 14;
        worksheet.Column(18).Width = 14;
    }

    private static void WriteHeader(IXLWorksheet worksheet, int topRow, int bottomRow)
    {
        worksheet.Range(topRow, 1, bottomRow, 1).Merge().Value = "STT";
        worksheet.Range(topRow, 2, bottomRow, 2).Merge().Value = "Tên sản phẩm";
        worksheet.Range(topRow, 3, bottomRow, 3).Merge().Value = "Đơn vị tính";
        worksheet.Range(topRow, 4, bottomRow, 4).Merge().Value = "Sản lượng";
        worksheet.Range(topRow, 5, bottomRow, 5).Merge().Value = "Tên Nhóm vật tư, tài sản";
        worksheet.Range(topRow, 6, bottomRow, 6).Merge().Value = "ĐVT";
        worksheet.Range(topRow, 7, bottomRow, 7).Merge().Value = "Số lượng";
        worksheet.Range(topRow, 8, bottomRow, 8).Merge().Value = "K1";
        worksheet.Range(topRow, 9, bottomRow, 9).Merge().Value = "K2";
        worksheet.Range(topRow, 10, bottomRow, 10).Merge().Value = "K3";
        worksheet.Range(topRow, 11, bottomRow, 11).Merge().Value = "K4";
        worksheet.Range(topRow, 12, bottomRow, 12).Merge().Value = "K5";
        worksheet.Range(topRow, 13, bottomRow, 13).Merge().Value = "K6";
        worksheet.Range(topRow, 14, bottomRow, 14).Merge().Value = "K7";

        worksheet.Range(topRow, 15, topRow, 16).Merge().Value = "SCTX";
        worksheet.Range(topRow, 17, topRow, 18).Merge().Value = "ĐIỆN NĂNG";

        worksheet.Cell(bottomRow, 15).Value = "Đơn giá";
        worksheet.Cell(bottomRow, 16).Value = "Thành tiền";
        worksheet.Cell(bottomRow, 17).Value = "Đơn giá";
        worksheet.Cell(bottomRow, 18).Value = "Thành tiền";

        var headerRange = worksheet.Range(topRow, 1, bottomRow, 18);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e6e6e6");
    }

    private static string ToRoman(int value)
    {
        var romans = new List<(string Roman, int Arabic)>
        {
            ("M", 1000), ("CM", 900), ("D", 500), ("CD", 400), ("C", 100),
            ("XC", 90), ("L", 50), ("XL", 40), ("X", 10), ("IX", 9),
            ("V", 5), ("IV", 4), ("I", 1)
        };

        var number = value;
        var result = string.Empty;

        foreach (var (roman, arabic) in romans)
        {
            while (number >= arabic)
            {
                result += roman;
                number -= arabic;
            }
        }

        return result;
    }

    private sealed record PeriodItem(
        Guid OutputId,
        DateOnly? StartMonth,
        DateOnly? EndMonth,
        double? ProductionMeters,
        double? StandardProductionMeters);

    private sealed record ReportBlock(
        string ProcessGroupLabel,
        string ProductName,
        string ProductUnitLabel,
        double ProductionMeters,
        List<ReportRow> Rows);

    private sealed record ReportRow(
        string Key,
        string AssignmentCodeName,
        string UnitOfMeasureName,
        double Quantity,
        double[] KValues,
        double? MaintainUnitPrice,
        double? MaintainTotalPrice,
        double? ElectricityUnitPrice,
        double? ElectricityTotalPrice);
}
