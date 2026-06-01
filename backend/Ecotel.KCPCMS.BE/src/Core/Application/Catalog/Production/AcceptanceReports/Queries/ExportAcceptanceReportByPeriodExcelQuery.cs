using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Production.ProductionOutputs.Queries;
using Application.Dto.Catalog.ProductionOutput;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Production;
using MediatR;
using System.Text;

#pragma warning disable IDE0007
#pragma warning disable IDE0008

namespace Application.Catalog.Production.AcceptanceReports.Queries;

public record ExportAcceptanceReportByPeriodExcelQuery(string? Month, string? Year)
    : IRequest<ExportAcceptanceReportByPeriodExcelResponse>;

public record ExportAcceptanceReportByPeriodExcelResponse(ReadOnlyMemory<byte> FileBytes, string FileName);

public class ExportAcceptanceReportByPeriodExcelQueryHandler(IUnitOfWork unitOfWork, IMediator mediator)
    : IRequestHandler<ExportAcceptanceReportByPeriodExcelQuery, ExportAcceptanceReportByPeriodExcelResponse>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();

    private const int TotalColumns = 43;

    public async Task<ExportAcceptanceReportByPeriodExcelResponse> Handle(
        ExportAcceptanceReportByPeriodExcelQuery request,
        CancellationToken cancellationToken)
    {
        var (month, year) = ResolveMonthYear(request.Month, request.Year);

        var outputs = await _productionOutputRepository.GetAllAsync(
            predicate: x => x.StartMonth.Month == month && x.StartMonth.Year == year && x.AcceptanceReport != null,
            orderBy: q => q
                .OrderByDescending(x => x.StartMonth)
                .ThenByDescending(x => x.Id),
            disableTracking: true);

        if (outputs.Count == 0)
        {
            throw new NotFoundException("Không tìm thấy dữ liệu nghiệm thu cho kỳ đã chọn.");
        }

        var details = new List<ProductionOutputDetailResponseDto>();
        foreach (var output in outputs)
        {
            var detail = await mediator.Send(new GetProductionOutputDetailQuery(output.Id), cancellationToken);
            details.Add(detail);
        }

        List<ReportRow> reportRows = BuildRows(details);
        byte[] fileBytes = BuildWorkbook(reportRows, month, year);
        string fileName = $"bang-nghiem-thu-vat-tu-su-dung-ket-chuyen-chi-phi-{month:D2}-{year}.xlsx";

        return new ExportAcceptanceReportByPeriodExcelResponse(fileBytes, fileName);
    }

    private static (int month, int year) ResolveMonthYear(string? monthText, string? yearText)
    {
        DateTime now = DateTime.Now;
        int month = now.Month;
        int year = now.Year;

        if (!string.IsNullOrWhiteSpace(monthText) && (!int.TryParse(monthText, out month) || month is < 1 or > 12))
        {
            throw new BadRequestException("Tháng không hợp lệ. Định dạng mong muốn: MM.");
        }

        if (!string.IsNullOrWhiteSpace(yearText) && (!int.TryParse(yearText, out year) || year < 1))
        {
            throw new BadRequestException("Năm không hợp lệ. Định dạng mong muốn: YYYY.");
        }

        return (month, year);
    }

    private static List<ReportRow> BuildRows(IReadOnlyList<ProductionOutputDetailResponseDto> details)
    {
        var rows = new List<ReportRow>();

        foreach (var detail in details)
        {
            AddCategoryRows(rows, "Vật tư đã tính vào doanh thu khoán", detail.SectionA, SectionKind.SectionA);
            AddCategoryRows(rows, "Bổ sung chi phí", detail.SectionB, SectionKind.SectionB);
            AddCategoryRows(rows, "Vật tư theo hạn mức", detail.SectionC, SectionKind.SectionC);
            AddCategoryRows(rows, "Tài sản", detail.SectionD, SectionKind.SectionD);
        }

        return rows;
    }

    private static void AddCategoryRows(
        IList<ReportRow> rows,
        string categoryName,
        IReadOnlyList<MaterialGroupDto> groups,
        SectionKind section)
    {
        if (groups.Count == 0)
        {
            return;
        }

        var typeGroups = groups
            .GroupBy(x => ResolveTypeName(x, section))
            .Select(g => new
            {
                TypeName = g.Key,
                TypeOrder = ResolveTypeOrder(g.First(), section),
                Groups = g.OrderBy(x => x.GroupCode).ThenBy(x => x.GroupName).ToList()
            })
            .OrderBy(x => x.TypeOrder)
            .ThenBy(x => x.TypeName)
            .ToList();

        var categoryItems = typeGroups.SelectMany(x => ExtractItems(x.Groups, section)).ToList();
        rows.Add(ReportRow.CreateCategory(categoryName, SumItems(categoryItems)));

        foreach (var typeGroup in typeGroups)
        {
            var typeItems = ExtractItems(typeGroup.Groups, section).ToList();
            rows.Add(ReportRow.CreateType(typeGroup.TypeName, SumItems(typeItems)));

            foreach (var group in typeGroup.Groups)
            {
                if (ShouldUseFlatItems(group, section))
                {
                    foreach (var item in group.Materials.Select(ToItemFinancialData))
                    {
                        rows.Add(ReportRow.CreateItem(item));
                    }

                    continue;
                }

                if (group.SubGroups.Count > 0)
                {
                    foreach (var subGroup in group.SubGroups)
                    {
                        if (subGroup.Materials.Count == 0)
                        {
                            continue;
                        }

                        string subGroupLabel = ResolveSubGroupName(subGroup.SubGroupCode);
                        var subGroupItems = subGroup.Materials.Select(ToItemFinancialData).ToList();
                        rows.Add(ReportRow.CreateGroup(subGroupLabel, SumItems(subGroupItems)));

                        foreach (var item in subGroupItems)
                        {
                            rows.Add(ReportRow.CreateItem(item));
                        }
                    }
                }

                if (group.Materials.Count > 0)
                {
                    string groupLabel = ResolveGroupName(group, section);
                    var groupItems = group.Materials.Select(ToItemFinancialData).ToList();
                    rows.Add(ReportRow.CreateGroup(groupLabel, SumItems(groupItems)));

                    foreach (var item in groupItems)
                    {
                        rows.Add(ReportRow.CreateItem(item));
                    }
                }
            }
        }
    }

    private static IEnumerable<ItemFinancialData> ExtractItems(IReadOnlyCollection<MaterialGroupDto> groups, SectionKind section)
    {
        foreach (var group in groups)
        {
            if (ShouldUseFlatItems(group, section))
            {
                foreach (var item in group.Materials)
                {
                    yield return ToItemFinancialData(item);
                }

                continue;
            }

            foreach (var subGroup in group.SubGroups)
            {
                foreach (var item in subGroup.Materials)
                {
                    yield return ToItemFinancialData(item);
                }
            }

            foreach (var item in group.Materials)
            {
                yield return ToItemFinancialData(item);
            }
        }
    }

    private static string ResolveTypeName(MaterialGroupDto group, SectionKind section)
    {
        if (section == SectionKind.SectionA)
        {
            return group.SectionAType switch
            {
                1 => "Vật liệu",
                2 => "Chi phí sửa chữa thường xuyên (Các loại vật tư SCTX theo kế hoạch vật tư)",
                3 => "Chi phí sửa chữa thường xuyên dài kỳ phân bổ",
                _ => group.MaterialType
            };
        }

        if (section == SectionKind.SectionB)
        {
            return group.AdditionalCostType switch
            {
                AdditionalCost.Material => "Vật liệu",
                AdditionalCost.Maintain => "Sửa chữa thường xuyên",
                AdditionalCost.SafeAndWelfare => "Vật tư theo chế độ người lao động, phòng cháy chữa cháy, phòng chống mưa bão",
                _ => group.MaterialType
            };
        }

        return group.MaterialType;
    }

    private static int ResolveTypeOrder(MaterialGroupDto group, SectionKind section)
    {
        if (section == SectionKind.SectionA)
        {
            return group.SectionAType ?? int.MaxValue;
        }

        if (section == SectionKind.SectionB)
        {
            return (int?)group.AdditionalCostType ?? int.MaxValue;
        }

        return int.MaxValue;
    }

    private static bool ShouldUseFlatItems(MaterialGroupDto group, SectionKind section)
    {
        if (section != SectionKind.SectionB)
        {
            return false;
        }

        string groupCode = (group.GroupCode ?? string.Empty).Trim().ToUpperInvariant();
        return group.AdditionalCostType == AdditionalCost.Material
            && group.ProductionOrderId == null
            && groupCode == "NO_ORDER";
    }

    private static string ResolveGroupName(MaterialGroupDto group, SectionKind section)
    {
        string groupCode = (group.GroupCode ?? string.Empty).Trim();
        string groupName = (group.GroupName ?? string.Empty).Trim();

        if (section == SectionKind.SectionA)
        {
            if (group.ProductionOrderId != null)
            {
                return string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;
            }

            if (group.SectionAType == 2 && string.Equals(groupCode, "VTK", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(groupName) ? "Vật tư khác" : groupName;
            }

            return string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;
        }

        if (section == SectionKind.SectionB)
        {
            if (group.AdditionalCostType == AdditionalCost.SafeAndWelfare)
            {
                return ResolveOtherMaterialLabel(group.OtherMaterialDetail)
                    ?? (string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName);
            }

            if (string.Equals(groupCode, "NO_ORDER", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            if (group.ProductionOrderId != null)
            {
                return string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;
            }

            return string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;
        }

        return string.IsNullOrWhiteSpace(groupName) ? groupCode : groupName;
    }

    private static string? ResolveOtherMaterialLabel(OtherMaterialDetail? detail)
    {
        return detail switch
        {
            OtherMaterialDetail.BaoHoLaoDong => "Bảo hộ lao động",
            OtherMaterialDetail.VatTuPhucVuCongTacAnToan => "Vật tư phục vụ công tác an toàn",
            _ => null
        };
    }

    private static string ResolveSubGroupName(string subGroupCode)
    {
        return subGroupCode switch
        {
            "New" => "Lĩnh mới",
            "Reusable" => "Lĩnh tái sử dụng",
            _ => subGroupCode
        };
    }

    private static ItemFinancialData ToItemFinancialData(MaterialDetailDto material)
    {
        return new ItemFinancialData
        {
            ItemCode = material.MaterialCode,
            ItemName = material.MaterialName,
            Unit = material.UnitOfMeasureName,
            PriceKH = material.PlannedUnitPrice,
            PriceTT = material.ActualUnitPrice,
            OpeningBalanceTotalQty = material.BeginningInventory?.Total.Quantity ?? 0,
            OpeningBalanceTotalAmount = material.BeginningInventory?.Total.Amount ?? 0,
            OpeningBalanceOnSiteQty = material.BeginningInventory?.RemainingAtSite?.Quantity ?? 0,
            OpeningBalanceOnSiteAmount = material.BeginningInventory?.RemainingAtSite?.Amount ?? 0,
            OpeningBalancePendingQty = 0,
            OpeningBalancePendingAmount = material.BeginningInventory?.PendingValue ?? 0,
            OpeningBalanceContractQty = material.BeginningInventory?.RemainingByOrder?.Quantity ?? 0,
            OpeningBalanceContractAmount = material.BeginningInventory?.RemainingByOrder?.Amount ?? 0,
            ReceiptTotalQty = material.IssuedInPeriod?.Total.Quantity ?? 0,
            ReceiptTotalAmountKH = material.IssuedInPeriod?.Total.Amount ?? 0,
            ReceiptWithReceiptQty = material.IssuedInPeriod?.Received.Quantity ?? 0,
            ReceiptWithReceiptAmountKH = material.IssuedInPeriod?.Received.PlannedAmount ?? 0,
            ReceiptWithReceiptAmountTT = material.IssuedInPeriod?.Received.ActualAmount ?? 0,
            ReceiptBorrowedQty = material.IssuedInPeriod?.BorrowedNoVoucher.Quantity ?? 0,
            ReceiptBorrowedAmount = material.IssuedInPeriod?.BorrowedNoVoucher.Amount ?? 0,
            ReceiptReturnPrevMonthQty = material.IssuedInPeriod?.ReturnPreviousMonthVoucher.Quantity ?? 0,
            ReceiptReturnPrevMonthAmount = material.IssuedInPeriod?.ReturnPreviousMonthVoucher.Amount ?? 0,
            ReceiptHandoverQty = material.IssuedInPeriod?.OtherReceipt.Quantity ?? 0,
            ReceiptHandoverAmount = material.IssuedInPeriod?.OtherReceipt.Amount ?? 0,
            IssueTotalQty = material.ExportedInPeriod?.Total.Quantity ?? 0,
            IssueTotalAmount = material.ExportedInPeriod?.Total.Amount ?? 0,
            IssueForProductionQty = material.ExportedInPeriod?.ExportedToProduction.Quantity ?? 0,
            IssueForProductionAmount = material.ExportedInPeriod?.ExportedToProduction.Amount ?? 0,
            IssueLongtermQty = material.ExportedInPeriod?.LongTermExpense?.Amount > 0 ? 1 : 0,
            IssueLongtermAmount = material.ExportedInPeriod?.LongTermExpense?.Amount ?? 0,
            IssueOtherQty = material.ExportedInPeriod?.OtherExport.Quantity ?? 0,
            IssueOtherAmount = material.ExportedInPeriod?.OtherExport.Amount ?? 0,
            IssueContractQty = material.ExportedInPeriod?.ContractSettlement.Quantity ?? 0,
            IssueContractAmount = material.ExportedInPeriod?.ContractSettlement.Amount ?? 0,
            ClosingBalanceTotalQty = material.EndingInventory?.Total.Quantity ?? 0,
            ClosingBalanceTotalAmount = material.EndingInventory?.Total.Amount ?? 0,
            ClosingBalanceOnSiteQty = material.EndingInventory?.RemainingAtSite?.Quantity ?? 0,
            ClosingBalanceOnSiteAmount = material.EndingInventory?.RemainingAtSite?.Amount ?? 0,
            ClosingBalancePendingQty = 0,
            ClosingBalancePendingAmount = material.EndingInventory?.PendingValue ?? 0,
            ClosingBalanceContractQty = material.EndingInventory?.RemainingByOrder?.Quantity ?? 0,
            ClosingBalanceContractAmount = material.EndingInventory?.RemainingByOrder?.Amount ?? 0
        };
    }

    private static ItemFinancialData SumItems(IReadOnlyList<ItemFinancialData> items)
    {
        var total = new ItemFinancialData();

        foreach (var item in items)
        {
            total.OpeningBalanceTotalQty += item.OpeningBalanceTotalQty;
            total.OpeningBalanceTotalAmount += item.OpeningBalanceTotalAmount;
            total.OpeningBalanceOnSiteQty += item.OpeningBalanceOnSiteQty;
            total.OpeningBalanceOnSiteAmount += item.OpeningBalanceOnSiteAmount;
            total.OpeningBalancePendingQty += item.OpeningBalancePendingQty;
            total.OpeningBalancePendingAmount += item.OpeningBalancePendingAmount;
            total.OpeningBalanceContractQty += item.OpeningBalanceContractQty;
            total.OpeningBalanceContractAmount += item.OpeningBalanceContractAmount;
            total.ReceiptTotalQty += item.ReceiptTotalQty;
            total.ReceiptTotalAmountKH += item.ReceiptTotalAmountKH;
            total.ReceiptWithReceiptQty += item.ReceiptWithReceiptQty;
            total.ReceiptWithReceiptAmountKH += item.ReceiptWithReceiptAmountKH;
            total.ReceiptWithReceiptAmountTT += item.ReceiptWithReceiptAmountTT;
            total.ReceiptBorrowedQty += item.ReceiptBorrowedQty;
            total.ReceiptBorrowedAmount += item.ReceiptBorrowedAmount;
            total.ReceiptReturnPrevMonthQty += item.ReceiptReturnPrevMonthQty;
            total.ReceiptReturnPrevMonthAmount += item.ReceiptReturnPrevMonthAmount;
            total.ReceiptHandoverQty += item.ReceiptHandoverQty;
            total.ReceiptHandoverAmount += item.ReceiptHandoverAmount;
            total.IssueTotalQty += item.IssueTotalQty;
            total.IssueTotalAmount += item.IssueTotalAmount;
            total.IssueForProductionQty += item.IssueForProductionQty;
            total.IssueForProductionAmount += item.IssueForProductionAmount;
            total.IssueLongtermQty += item.IssueLongtermQty;
            total.IssueLongtermAmount += item.IssueLongtermAmount;
            total.IssueOtherQty += item.IssueOtherQty;
            total.IssueOtherAmount += item.IssueOtherAmount;
            total.IssueContractQty += item.IssueContractQty;
            total.IssueContractAmount += item.IssueContractAmount;
            total.ClosingBalanceTotalQty += item.ClosingBalanceTotalQty;
            total.ClosingBalanceTotalAmount += item.ClosingBalanceTotalAmount;
            total.ClosingBalanceOnSiteQty += item.ClosingBalanceOnSiteQty;
            total.ClosingBalanceOnSiteAmount += item.ClosingBalanceOnSiteAmount;
            total.ClosingBalancePendingQty += item.ClosingBalancePendingQty;
            total.ClosingBalancePendingAmount += item.ClosingBalancePendingAmount;
            total.ClosingBalanceContractQty += item.ClosingBalanceContractQty;
            total.ClosingBalanceContractAmount += item.ClosingBalanceContractAmount;
        }

        return total;
    }

    private static byte[] BuildWorkbook(IReadOnlyList<ReportRow> rows, int month, int year)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Bang nghiem thu");

        worksheet.Style.Font.FontName = "Times New Roman";
        worksheet.Style.Font.FontSize = 11;

        ConfigureColumnWidths(worksheet);
        WriteHeader(worksheet, month, year);
        WriteTableHeader(worksheet);
        int tableStartRow = 12;
        int tableEndRow = WriteRows(worksheet, tableStartRow, rows);
        WriteFooter(worksheet, tableEndRow + 2, month, year);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ConfigureColumnWidths(IXLWorksheet worksheet)
    {
        worksheet.Column(1).Width = 7;
        worksheet.Column(2).Width = 38;
        worksheet.Column(3).Width = 8;
        worksheet.Column(4).Width = 14;

        worksheet.Column(5).Width = 13;
        worksheet.Column(6).Width = 13;

        for (int column = 7; column <= TotalColumns; column++)
        {
            worksheet.Column(column).Width = column % 2 == 1 ? 8 : 14;
        }
    }

    private static void WriteHeader(IXLWorksheet worksheet, int month, int year)
    {
        worksheet.Range(1, 1, 1, 14).Merge();
        worksheet.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN THAN HÀ LẦM - VINACOMIN";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(2, 1, 2, 14).Merge();
        worksheet.Cell(2, 1).Value = "CÔNG TRƯỜNG KHAI THÁC 1";
        worksheet.Cell(2, 1).Style.Font.Bold = true;
        worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(2, 1).Style.Font.Underline = XLFontUnderlineValues.Single;

        worksheet.Range(4, 1, 4, TotalColumns).Merge();
        worksheet.Cell(4, 1).Value = "BẢNG NGHIỆM THU VẬT TƯ SỬ DỤNG VÀ KẾT CHUYỂN CHI PHÍ";
        worksheet.Cell(4, 1).Style.Font.Bold = true;
        worksheet.Cell(4, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        worksheet.Range(5, 1, 5, TotalColumns).Merge();
        worksheet.Cell(5, 1).Value = $"THÁNG {month} NĂM {year}";
        worksheet.Cell(5, 1).Style.Font.Bold = true;
        worksheet.Cell(5, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void WriteTableHeader(IXLWorksheet worksheet)
    {
        int row1 = 7;
        int row2 = 8;
        int row3 = 9;

        worksheet.Range(row1, 1, row3, 1).Merge().Value = "STT";
        worksheet.Range(row1, 2, row3, 2).Merge().Value = "DANH MỤC VẬT TƯ, HÀNG HÓA";
        worksheet.Range(row1, 3, row3, 3).Merge().Value = "ĐVT";
        worksheet.Range(row1, 4, row3, 4).Merge().Value = "CÁCH TÍNH";

        worksheet.Range(row1, 5, row1, 6).Merge().Value = "ĐƠN GIÁ";
        worksheet.Range(row1, 7, row1, 14).Merge().Value = "TỒN ĐẦU KỲ";
        worksheet.Range(row1, 15, row1, 25).Merge().Value = "LĨNH TRONG KỲ";
        worksheet.Range(row1, 26, row1, 35).Merge().Value = "XUẤT TRONG KỲ";
        worksheet.Range(row1, 36, row1, 43).Merge().Value = "TỒN CUỐI KỲ";

        worksheet.Range(row2, 5, row3, 5).Merge().Value = "Kế hoạch";
        worksheet.Range(row2, 6, row3, 6).Merge().Value = "Thực tế";

        worksheet.Range(row2, 7, row2, 8).Merge().Value = "Tổng cộng";
        worksheet.Range(row2, 9, row2, 10).Merge().Value = "Tồn tại khai trường";
        worksheet.Range(row2, 11, row2, 12).Merge().Value = "Chi phí chờ hạch toán";
        worksheet.Range(row2, 13, row2, 14).Merge().Value = "Quyết định, giao khoán công trình";

        worksheet.Range(row2, 15, row2, 16).Merge().Value = "Tổng cộng";
        worksheet.Range(row2, 17, row2, 19).Merge().Value = "Lĩnh vật tư (Trả phiếu)";
        worksheet.Range(row2, 20, row2, 21).Merge().Value = "Vay chưa trả phiếu";
        worksheet.Range(row2, 22, row2, 23).Merge().Value = "Trả phiếu tháng trước";
        worksheet.Range(row2, 24, row2, 25).Merge().Value = "Lĩnh khác";

        worksheet.Range(row2, 26, row2, 27).Merge().Value = "Tổng cộng";
        worksheet.Range(row2, 28, row2, 29).Merge().Value = "Xuất cho sản xuất";
        worksheet.Range(row2, 30, row2, 31).Merge().Value = "Chi phí vật tư dài kỳ hạch toán";
        worksheet.Range(row2, 32, row2, 33).Merge().Value = "Xuất khác";
        worksheet.Range(row2, 34, row2, 35).Merge().Value = "Quyết định, giao khoán công trình";

        worksheet.Range(row2, 36, row2, 37).Merge().Value = "Tổng cộng";
        worksheet.Range(row2, 38, row2, 39).Merge().Value = "Tồn tại khai trường";
        worksheet.Range(row2, 40, row2, 41).Merge().Value = "Chi phí chờ hạch toán";
        worksheet.Range(row2, 42, row2, 43).Merge().Value = "Quyết định, giao khoán công trình";

        WriteQtyAmountPair(worksheet, row3, 7, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 9, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 11, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 13, "SL", "Thành tiền");

        WriteQtyAmountPair(worksheet, row3, 15, "SL", "Thành tiền KH");
        worksheet.Cell(row3, 17).Value = "SL";
        worksheet.Cell(row3, 18).Value = "Thành tiền KH";
        worksheet.Cell(row3, 19).Value = "Thành tiền TT";
        WriteQtyAmountPair(worksheet, row3, 20, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 22, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 24, "SL", "Thành tiền");

        WriteQtyAmountPair(worksheet, row3, 26, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 28, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 30, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 32, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 34, "SL", "Thành tiền");

        WriteQtyAmountPair(worksheet, row3, 36, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 38, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 40, "SL", "Thành tiền");
        WriteQtyAmountPair(worksheet, row3, 42, "SL", "Thành tiền");

        IXLRange headerRange = worksheet.Range(row1, 1, row3, TotalColumns);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Alignment.WrapText = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e6e6e6");
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
    }

    private static void WriteQtyAmountPair(IXLWorksheet worksheet, int row, int startColumn, string qtyText, string amountText)
    {
        worksheet.Cell(row, startColumn).Value = qtyText;
        worksheet.Cell(row, startColumn + 1).Value = amountText;
    }

    private static int WriteRows(IXLWorksheet worksheet, int startRow, IReadOnlyList<ReportRow> rows)
    {
        int rowIndex = startRow;
        int categoryIndex = 0;
        int typeIndex = 0;
        int groupIndex = 0;

        foreach (var row in rows)
        {
            switch (row.RowType)
            {
                case RowType.Category:
                    categoryIndex++;
                    typeIndex = 0;
                    groupIndex = 0;
                    worksheet.Cell(rowIndex, 1).Value = $"{(char)('A' + categoryIndex - 1)}.";
                    break;
                case RowType.Type:
                    typeIndex++;
                    groupIndex = 0;
                    worksheet.Cell(rowIndex, 1).Value = $"{ToRoman(typeIndex)}.";
                    break;
                case RowType.Group:
                    groupIndex++;
                    worksheet.Cell(rowIndex, 1).Value = $"{ToRoman(typeIndex)}.{groupIndex}";
                    break;
                default:
                    worksheet.Cell(rowIndex, 1).Value = string.Empty;
                    break;
            }

            IXLCell labelCell = worksheet.Cell(rowIndex, 2);
            if (row.RowType == RowType.Item)
            {
                labelCell.Value = string.IsNullOrWhiteSpace(row.Data.ItemCode)
                    ? row.Data.ItemName
                    : $"{row.Data.ItemCode} - {row.Data.ItemName}";
            }
            else
            {
                labelCell.Value = row.Label;
            }

            worksheet.Cell(rowIndex, 3).Value = row.RowType == RowType.Item ? row.Data.Unit : string.Empty;
            worksheet.Cell(rowIndex, 4).Value = string.Empty;

            WriteFinancialValues(worksheet, rowIndex, row.Data, row.RowType == RowType.Item);
            ApplyRowStyle(worksheet, rowIndex, row.RowType);

            rowIndex++;
        }

        if (rows.Count > 0)
        {
            IXLRange bodyRange = worksheet.Range(startRow, 1, rowIndex - 1, TotalColumns);
            bodyRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            bodyRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            bodyRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            bodyRange.Style.Alignment.WrapText = true;
        }

        return Math.Max(startRow, rowIndex - 1);
    }

    private static void WriteFinancialValues(IXLWorksheet worksheet, int row, ItemFinancialData data, bool includePrice)
    {
        if (includePrice)
        {
            worksheet.Cell(row, 5).Value = data.PriceKH;
            worksheet.Cell(row, 6).Value = data.PriceTT;
            worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
        }

        WriteQtyAmount(worksheet, row, 7, data.OpeningBalanceTotalQty, data.OpeningBalanceTotalAmount);
        WriteQtyAmount(worksheet, row, 9, data.OpeningBalanceOnSiteQty, data.OpeningBalanceOnSiteAmount);
        WriteQtyAmount(worksheet, row, 11, data.OpeningBalancePendingQty, data.OpeningBalancePendingAmount);
        WriteQtyAmount(worksheet, row, 13, data.OpeningBalanceContractQty, data.OpeningBalanceContractAmount);

        WriteQtyAmount(worksheet, row, 15, data.ReceiptTotalQty, data.ReceiptTotalAmountKH);
        worksheet.Cell(row, 17).Value = data.ReceiptWithReceiptQty;
        worksheet.Cell(row, 18).Value = data.ReceiptWithReceiptAmountKH;
        worksheet.Cell(row, 19).Value = data.ReceiptWithReceiptAmountTT;
        WriteQtyAmount(worksheet, row, 20, data.ReceiptBorrowedQty, data.ReceiptBorrowedAmount);
        WriteQtyAmount(worksheet, row, 22, data.ReceiptReturnPrevMonthQty, data.ReceiptReturnPrevMonthAmount);
        WriteQtyAmount(worksheet, row, 24, data.ReceiptHandoverQty, data.ReceiptHandoverAmount);

        WriteQtyAmount(worksheet, row, 26, data.IssueTotalQty, data.IssueTotalAmount);
        WriteQtyAmount(worksheet, row, 28, data.IssueForProductionQty, data.IssueForProductionAmount);
        WriteQtyAmount(worksheet, row, 30, data.IssueLongtermQty, data.IssueLongtermAmount);
        WriteQtyAmount(worksheet, row, 32, data.IssueOtherQty, data.IssueOtherAmount);
        WriteQtyAmount(worksheet, row, 34, data.IssueContractQty, data.IssueContractAmount);

        WriteQtyAmount(worksheet, row, 36, data.ClosingBalanceTotalQty, data.ClosingBalanceTotalAmount);
        WriteQtyAmount(worksheet, row, 38, data.ClosingBalanceOnSiteQty, data.ClosingBalanceOnSiteAmount);
        WriteQtyAmount(worksheet, row, 40, data.ClosingBalancePendingQty, data.ClosingBalancePendingAmount);
        WriteQtyAmount(worksheet, row, 42, data.ClosingBalanceContractQty, data.ClosingBalanceContractAmount);

        for (int column = 7; column <= TotalColumns; column += 2)
        {
            worksheet.Cell(row, column).Style.NumberFormat.Format = "0.###";
            worksheet.Cell(row, column + 1).Style.NumberFormat.Format = "#,##0";
        }

        for (int column = 5; column <= TotalColumns; column++)
        {
            worksheet.Cell(row, column).Style.Alignment.ShrinkToFit = true;
        }

        worksheet.Cell(row, 18).Style.NumberFormat.Format = "#,##0";
        worksheet.Cell(row, 19).Style.NumberFormat.Format = "#,##0";
    }

    private static void WriteQtyAmount(IXLWorksheet worksheet, int row, int startColumn, double qty, decimal amount)
    {
        worksheet.Cell(row, startColumn).Value = qty;
        worksheet.Cell(row, startColumn + 1).Value = amount;
    }

    private static void ApplyRowStyle(IXLWorksheet worksheet, int row, RowType rowType)
    {
        IXLRange rowRange = worksheet.Range(row, 1, row, TotalColumns);

        switch (rowType)
        {
            case RowType.Category:
                rowRange.Style.Font.Bold = true;
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#e2f0d9");
                break;
            case RowType.Type:
                rowRange.Style.Font.Bold = true;
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f2f2f2");
                break;
            case RowType.Group:
                rowRange.Style.Font.Bold = true;
                rowRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#fbfbfb");
                break;
            case RowType.Item:
                break;
        }

        worksheet.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
        worksheet.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        for (int column = 5; column <= TotalColumns; column++)
        {
            worksheet.Cell(row, column).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }
    }

    private static void WriteFooter(IXLWorksheet worksheet, int startRow, int month, int year)
    {
        worksheet.Range(startRow, 1, startRow, TotalColumns).Merge();
        worksheet.Cell(startRow, 1).Value = $"Hà lầm, tháng {month} năm {year}";
        worksheet.Cell(startRow, 1).Style.Font.Italic = true;
        worksheet.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        worksheet.Range(startRow + 2, 1, startRow + 3, TotalColumns).Merge();
        worksheet.Cell(startRow + 2, 1).Value =
            "Kết luận: Toàn bộ số vật tư trên đã được sử dụng đúng mục đích, đảm bảo kỹ thuật an toàn. Hội đồng nghiệm thu thống nhất nghiệm thu làm cơ sở thanh toán.";
        worksheet.Cell(startRow + 2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(startRow + 2, 1).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Cell(startRow + 2, 1).Style.Alignment.WrapText = true;
        worksheet.Range(startRow + 2, 1, startRow + 3, TotalColumns).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        worksheet.Range(startRow + 5, 1, startRow + 5, 22).Merge().Value = "ĐẠI DIỆN BÊN NHẬN KHOÁN";
        worksheet.Range(startRow + 5, 23, startRow + 5, TotalColumns).Merge().Value = "ĐẠI DIỆN BÊN GIAO KHOÁN";

        worksheet.Range(startRow + 6, 23, startRow + 6, TotalColumns).Merge().Value = "KT.GIÁM ĐỐC";
        worksheet.Range(startRow + 7, 23, startRow + 7, TotalColumns).Merge().Value = "PHÓ GIÁM ĐỐC";

        worksheet.Range(startRow + 7, 1, startRow + 7, 4).Merge().Value = "NGƯỜI LẬP";
        worksheet.Range(startRow + 7, 6, startRow + 7, 9).Merge().Value = "QUẢN ĐỐC";
        worksheet.Range(startRow + 7, 11, startRow + 7, 14).Merge().Value = "PHÒNG KH";
        worksheet.Range(startRow + 7, 16, startRow + 7, 19).Merge().Value = "PHÒNG KTTC";
        worksheet.Range(startRow + 7, 20, startRow + 7, 22).Merge().Value = "PHÒNG CV";

        worksheet.Range(startRow + 5, 1, startRow + 7, TotalColumns).Style.Font.Bold = true;
        worksheet.Range(startRow + 5, 1, startRow + 7, TotalColumns).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(startRow + 5, 1, startRow + 7, TotalColumns).Style.Alignment.WrapText = true;

        worksheet.Row(startRow + 8).Height = 28;
        worksheet.Row(startRow + 9).Height = 28;
        worksheet.Row(startRow + 10).Height = 28;
        worksheet.Row(startRow + 11).Height = 28;
    }

    private static string ToRoman(int number)
    {
        if (number <= 0)
        {
            return string.Empty;
        }

        (int Value, string Symbol)[] map =
        [
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
            (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
            (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        ];

        int remaining = number;
        StringBuilder result = new();

        foreach ((int value, string symbol) in map)
        {
            while (remaining >= value)
            {
                result.Append(symbol);
                remaining -= value;
            }
        }

        return result.ToString();
    }

    private enum SectionKind
    {
        SectionA,
        SectionB,
        SectionC,
        SectionD
    }

    private enum RowType
    {
        Category,
        Type,
        Group,
        Item
    }

    private sealed class ReportRow
    {
        public RowType RowType { get; init; }
        public string Label { get; init; } = string.Empty;
        public ItemFinancialData Data { get; init; } = new();

        public static ReportRow CreateCategory(string label, ItemFinancialData data) => new()
        {
            RowType = RowType.Category,
            Label = label,
            Data = data
        };

        public static ReportRow CreateType(string label, ItemFinancialData data) => new()
        {
            RowType = RowType.Type,
            Label = label,
            Data = data
        };

        public static ReportRow CreateGroup(string label, ItemFinancialData data) => new()
        {
            RowType = RowType.Group,
            Label = label,
            Data = data
        };

        public static ReportRow CreateItem(ItemFinancialData data) => new()
        {
            RowType = RowType.Item,
            Data = data
        };
    }

    private sealed class ItemFinancialData
    {
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public decimal PriceKH { get; set; }
        public decimal PriceTT { get; set; }

        public double OpeningBalanceTotalQty { get; set; }
        public decimal OpeningBalanceTotalAmount { get; set; }
        public double OpeningBalanceOnSiteQty { get; set; }
        public decimal OpeningBalanceOnSiteAmount { get; set; }
        public double OpeningBalancePendingQty { get; set; }
        public decimal OpeningBalancePendingAmount { get; set; }
        public double OpeningBalanceContractQty { get; set; }
        public decimal OpeningBalanceContractAmount { get; set; }

        public double ReceiptTotalQty { get; set; }
        public decimal ReceiptTotalAmountKH { get; set; }
        public double ReceiptWithReceiptQty { get; set; }
        public decimal ReceiptWithReceiptAmountKH { get; set; }
        public decimal ReceiptWithReceiptAmountTT { get; set; }
        public double ReceiptBorrowedQty { get; set; }
        public decimal ReceiptBorrowedAmount { get; set; }
        public double ReceiptReturnPrevMonthQty { get; set; }
        public decimal ReceiptReturnPrevMonthAmount { get; set; }
        public double ReceiptHandoverQty { get; set; }
        public decimal ReceiptHandoverAmount { get; set; }

        public double IssueTotalQty { get; set; }
        public decimal IssueTotalAmount { get; set; }
        public double IssueForProductionQty { get; set; }
        public decimal IssueForProductionAmount { get; set; }
        public double IssueLongtermQty { get; set; }
        public decimal IssueLongtermAmount { get; set; }
        public double IssueOtherQty { get; set; }
        public decimal IssueOtherAmount { get; set; }
        public double IssueContractQty { get; set; }
        public decimal IssueContractAmount { get; set; }

        public double ClosingBalanceTotalQty { get; set; }
        public decimal ClosingBalanceTotalAmount { get; set; }
        public double ClosingBalanceOnSiteQty { get; set; }
        public decimal ClosingBalanceOnSiteAmount { get; set; }
        public double ClosingBalancePendingQty { get; set; }
        public decimal ClosingBalancePendingAmount { get; set; }
        public double ClosingBalanceContractQty { get; set; }
        public decimal ClosingBalanceContractAmount { get; set; }
    }
}
