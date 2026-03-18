using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Queries;

public record ExportExcelTunnelMaintainUnitPriceEquipmentQuery() : IRequest<byte[]>;

public class ExportExcelTunnelMaintainUnitPriceEquipmentQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ExportExcelTunnelMaintainUnitPriceEquipmentQuery, byte[]>
{
    private readonly IWriteRepository<MaintainUnitPrice> _repository = unitOfWork.GetRepository<MaintainUnitPrice>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<byte[]> Handle(ExportExcelTunnelMaintainUnitPriceEquipmentQuery request, CancellationToken cancellationToken)
    {
        var list = await _repository.GetAllAsync(
            predicate: m => m.Type == MaintainUnitPriceType.TunnelExcavation,
            include: m => m
                .Include(m => m.Equipment)
                    .ThenInclude(e => e!.Code)
                .Include(m => m.MaintainUnitPriceEquipments)
                    .ThenInclude(mpe => mpe.Part)
                    .ThenInclude(p => p!.Code)
                .Include(m => m.MaintainUnitPriceEquipments)
                    .ThenInclude(mpe => mpe.Part)
                    .ThenInclude(p => p!.UnitOfMeasure),
            disableTracking: true);

        // Get dropdown data
        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e.Include(e => e.Code),
            selector: e => e.Code != null ? e.Code.Value : "",
            disableTracking: true);

        // Get all parts from database with Equipment relationship
        var allParts = await _partRepository.GetAllAsync(
            include: p => p.Include(p => p.Code).Include(p => p.UnitOfMeasure).Include(p => p.Equipment).ThenInclude(e => e!.Code),
            disableTracking: true);

        return ExportTransposedFormat(list.ToList(), equipments.Where(c => !string.IsNullOrEmpty(c)).ToList(), allParts.OrderBy(p => p.Equipment?.Code?.Value).ThenBy(p => p.Code?.Value).ToList());
    }

    private byte[] ExportTransposedFormat(
        List<MaintainUnitPrice> maintainUnitPrices,
        List<string> equipmentCodes,
        List<Part> allParts)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức bảo dưỡng lò đào");

        // Create hidden sheet for dropdown data
        var sourceSheet = workbook.Worksheets.Add("DataSources");
        sourceSheet.Hide();

        // Write dropdown data
        for (int i = 0; i < equipmentCodes.Count; i++)
        {
            sourceSheet.Cell(i + 1, 1).Value = equipmentCodes[i];
        }

        // Group parts by equipment from database relationship
        var partsByEquipment = allParts
            .Where(p => p.Equipment != null && p.Equipment.Code != null)
            .GroupBy(p => new { EquipmentId = p.EquipmentId, EquipmentCode = p.Equipment!.Code!.Value })
            .OrderBy(g => g.Key.EquipmentCode)
            .ToList();

        int currentRow = 1;
        int currentCol = 5; // Start from column E

        // Write metadata rows in Column C (row 1 = StartMonth label, row 2 = EndMonth label)
        var metadataLabels = new[] { "Thời gian bắt đầu", "Thời gian kết thúc" };

        for (int i = 0; i < metadataLabels.Length; i++)
        {
            var labelCell = worksheet.Cell(i + 1, 3); // Column C
            labelCell.Value = metadataLabels[i];
            labelCell.Style.Font.Bold = true;
            labelCell.Style.Fill.BackgroundColor = XLColor.LightGray;
            labelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }

        var timeGroups = maintainUnitPrices
            .GroupBy(m => new { m.StartMonth, m.EndMonth })
            .OrderBy(g => g.Key.StartMonth)
            .ToList();

        // Write each time group as 3 columns
        foreach (var timeGroup in timeGroups)
        {
            // Row 1: StartMonth
            worksheet.Cell(1, currentCol).Value = timeGroup.Key.StartMonth.ToString("MM/yyyy");
            worksheet.Cell(1, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Cell(1, currentCol).Style.Font.Bold = true;
            worksheet.Range(1, currentCol, 1, currentCol + 2).Merge();

            // Row 2: EndMonth
            worksheet.Cell(2, currentCol).Value = timeGroup.Key.EndMonth.ToString("MM/yyyy");
            worksheet.Cell(2, currentCol).Style.Fill.BackgroundColor = XLColor.LightBlue;
            worksheet.Cell(2, currentCol).Style.Font.Bold = true;
            worksheet.Range(2, currentCol, 2, currentCol + 2).Merge();

            currentCol += 3;
        }

        // Freeze panes (now 3 rows: row1=StartMonth, row2=EndMonth, row3=sub-headers)
        worksheet.SheetView.FreezeRows(3);
        worksheet.SheetView.FreezeColumns(4);

        // Write part data header (row 3)
        currentRow = 3;

        // Column A: Part Id (hidden)
        var partIdLabelCell = worksheet.Cell(currentRow, 1);
        partIdLabelCell.Value = "Id phụ tùng";
        partIdLabelCell.Style.Font.Bold = true;
        partIdLabelCell.Style.Fill.BackgroundColor = XLColor.LightGray;
        partIdLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Column B: Equipment Code
        var equipmentLabelCell = worksheet.Cell(currentRow, 2);
        equipmentLabelCell.Value = "Mã thiết bị";
        equipmentLabelCell.Style.Font.Bold = true;
        equipmentLabelCell.Style.Fill.BackgroundColor = XLColor.LightGray;
        equipmentLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Column C: Part Code
        var partLabelCell = worksheet.Cell(currentRow, 3);
        partLabelCell.Value = "Mã phụ tùng";
        partLabelCell.Style.Font.Bold = true;
        partLabelCell.Style.Fill.BackgroundColor = XLColor.LightGray;
        partLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Column D: Unit of Measure
        var unitLabelCell = worksheet.Cell(currentRow, 4);
        unitLabelCell.Value = "Đơn vị tính";
        unitLabelCell.Style.Font.Bold = true;
        unitLabelCell.Style.Fill.BackgroundColor = XLColor.LightGray;
        unitLabelCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Write headers for each time group (3 columns each, starting from column E)
        currentCol = 5;
        foreach (var _ in timeGroups)
        {
            worksheet.Cell(currentRow, currentCol).Value = "Định mức thời gian thay thế (tháng)";
            worksheet.Cell(currentRow, currentCol + 1).Value = "Số lượng vật tư 1 lần thay thế";
            worksheet.Cell(currentRow, currentCol + 2).Value = "Sản lượng than bình quân tháng";

            for (int i = 0; i < 3; i++)
            {
                worksheet.Cell(currentRow, currentCol + i).Style.Font.Bold = true;
                worksheet.Cell(currentRow, currentCol + i).Style.Fill.BackgroundColor = XLColor.LightYellow;
                worksheet.Cell(currentRow, currentCol + i).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                worksheet.Cell(currentRow, currentCol + i).Style.Alignment.WrapText = true;
            }

            currentCol += 3;
        }

        currentRow++; // Now at row 4, first data row

        // Add dropdown validation for Equipment Code column (B), data starts from row 4
        if (equipmentCodes.Any())
        {
            var equipmentRange = sourceSheet.Range(1, 1, equipmentCodes.Count, 1);
            var equipmentTargetRange = worksheet.Range(4, 2, Math.Max(currentRow + allParts.Count, 200), 2);
            var equipmentValidation = equipmentTargetRange.CreateDataValidation();
            equipmentValidation.List(equipmentRange);
            equipmentValidation.IgnoreBlanks = true;
            equipmentValidation.InCellDropdown = true;
        }

        // Hide Part Id column (column A)
        worksheet.Column(1).Hide();

        // Write part data grouped by equipment (from DB relationship)
        foreach (var equipmentGroup in partsByEquipment)
        {
            var equipmentCode = equipmentGroup.Key.EquipmentCode;
            var parts = equipmentGroup.OrderBy(p => p.Code?.Value).ToList();
            int groupStartRow = currentRow;

            foreach (var part in parts)
            {
                // Equipment code in column B
                var equipmentCell = worksheet.Cell(currentRow, 2);
                equipmentCell.Value = equipmentCode;
                equipmentCell.Style.Fill.BackgroundColor = XLColor.FromArgb(217, 217, 217);
                equipmentCell.Style.Font.Bold = true;
                equipmentCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                equipmentCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Part Id in column A (hidden)
                var partIdCell = worksheet.Cell(currentRow, 1);
                partIdCell.Value = part.Id.ToString();
                partIdCell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204);

                // Part code in column C
                var partCell = worksheet.Cell(currentRow, 3);
                partCell.Value = part.Code?.Value ?? "";
                partCell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204);

                // Unit in column D
                var unitCell = worksheet.Cell(currentRow, 4);
                unitCell.Value = part.UnitOfMeasure?.Name ?? "";
                unitCell.Style.Fill.BackgroundColor = XLColor.FromArgb(255, 242, 204);

                // Data for each time group (starting from column E)
                currentCol = 5;
                foreach (var timeGroup in timeGroups)
                {
                    var maintainUnitPrice = timeGroup
                        .FirstOrDefault(m => m.EquipmentId == equipmentGroup.Key.EquipmentId);

                    var partEquipment = maintainUnitPrice?.MaintainUnitPriceEquipments
                        .FirstOrDefault(mpe => mpe.PartId == part.Id);

                    if (partEquipment != null)
                    {
                        worksheet.Cell(currentRow, currentCol).Value = (double)partEquipment.ReplacementTimeStandard;
                        worksheet.Cell(currentRow, currentCol + 1).Value = partEquipment.Quantity;
                        worksheet.Cell(currentRow, currentCol + 2).Value = (double)partEquipment.AverageMonthlyTunnelProduction;

                        worksheet.Cell(currentRow, currentCol).Style.NumberFormat.Format = "0.00";
                        worksheet.Cell(currentRow, currentCol + 1).Style.NumberFormat.Format = "0.00";
                        worksheet.Cell(currentRow, currentCol + 2).Style.NumberFormat.Format = "0.00";
                    }
                    else
                    {
                        worksheet.Cell(currentRow, currentCol).Value = "";
                        worksheet.Cell(currentRow, currentCol + 1).Value = "";
                        worksheet.Cell(currentRow, currentCol + 2).Value = "";
                    }

                    currentCol += 3;
                }

                currentRow++;
            }

            // Merge equipment cells
            if (parts.Count > 1)
            {
                worksheet.Range(groupStartRow, 2, currentRow - 1, 2).Merge();
            }

            // Add border to separate equipment groups (column B only)
            var lastRowOfGroup = currentRow - 1;
            var groupBottomBorder = worksheet.Cell(lastRowOfGroup, 2);
            groupBottomBorder.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            groupBottomBorder.Style.Border.BottomBorderColor = XLColor.Black;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Save
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}