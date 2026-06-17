using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.Globalization;
using LongwallMaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record ExportExcelLongwallMaterialUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelLongwallMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelLongwallMaterialUnitPriceQuery, byte[]>
{
    private const string OtherMaterialDisplay = "VTK";

    private readonly IWriteRepository<LongwallMaterialUnitPriceEntity> _materialUnitPriceRepository = unitOfWork.GetRepository<LongwallMaterialUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Power> _powerRepository = unitOfWork.GetRepository<Power>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<Domain.Entities.Index.Material> _materialRepository = unitOfWork.GetRepository<Domain.Entities.Index.Material>();

    public async Task<byte[]> Handle(ExportExcelLongwallMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var list = await _materialUnitPriceRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProductionProcess)
                .Include(s => s.LongwallParameters)
                .Include(s => s.CuttingThickness)
                .Include(s => s.SeamFace)
                .Include(s => s.Technology)
                .Include(s => s.Hardness)
                .Include(s => s.Power)
                .Include(s => s.Code)
                .Include(s => s.MaterialUnitPriceAssignmentCodes)
                    .ThenInclude(c => c.AssignmentCode)
                        .ThenInclude(a => a.Code)
                .Include(s => s.MaterialUnitPriceAssignmentCodes)
                    .ThenInclude(c => c.Material)
                        .ThenInclude(m => m.Code),
            disableTracking: true);

        // --- LẤY DANH SÁCH CHO DROPDOWN CỐ ĐỊNH PHÍA TRƯỚC ---
        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);
        var hardnessOptions = (await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => ExtractLeadingNumber(x)).ToList();
        var powerOptions = (await _powerRepository.GetAllAsync(selector: p => p.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(selector: c => c.Value, disableTracking: true);
        var ctOptions = cuttingThicknesses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => ExtractLeadingNumber(x)).ToList();

        var longwallParams = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var longwallParamsOptions = longwallParams
            .Select(l => $"{l.Llc}-{l.Lkc}-{l.Mk}").Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var seamFaceEntities = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var seamFaceNames = seamFaceEntities.Select(s => s.Value)
            .OrderBy(v => ExtractLeadingNumber(v)).ThenBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

        var assignments = await _assignmentCodeRepository.GetAllAsync(include: a => a.Include(x => x.Code), disableTracking: true);
        var materials = await _materialRepository.GetAllAsync(include: m => m.Include(x => x.Code), disableTracking: true);

        var groupOptions = assignments.Select(a => a.Code?.Value?.Trim() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var materialCodeOptions = materials.Select(m => m.Code?.Value?.Trim() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var groupDisplayList = assignments
            .Where(a => !string.IsNullOrWhiteSpace(a.Code?.Value))
            .Select(a => string.IsNullOrWhiteSpace(a.Name) ? a.Code!.Value.Trim() : $"{a.Code.Value.Trim()} - {a.Name.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var matDisplayList = materials
            .Where(m => !string.IsNullOrWhiteSpace(m.Code?.Value))
            .Select(m => string.IsNullOrWhiteSpace(m.Name) ? m.Code!.Value.Trim() : $"{m.Code.Value.Trim()} - {m.Name.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        // --- KHỞI TẠO CẤU TRÚC ĐÚNG 8 CỘT MẶC ĐỊNH MAP TRỰC TIẾP TỪ DB ---
        var defaultColumns = new List<(string GroupCode, string MatCode)>
        {
            ("CKT", "CKT-CKT"),
            ("MKT", "MKT-MKT"),
            ("TN", "TNLT-TNLT"),
            ("KD-KVSAT", "KD-KD"),
            ("DNH", "DNH-DNH"),
            ("DB", "DB-DB"),
            ("LT", "LT-LT"),
            ("DT", "DT-DT")
        };

        var columnDefs = new List<(string GroupName, string MatCode, string GroupDisplay, string MatDisplay)>();

        foreach (var col in defaultColumns)
        {
            // Tìm nhóm chuẩn từ DB (Cấp 1)
            var groupEntity = assignments.FirstOrDefault(a => a.Code?.Value?.Trim().Equals(col.GroupCode, StringComparison.OrdinalIgnoreCase) == true);
            string groupName = groupEntity?.Code?.Value?.Trim() ?? col.GroupCode;
            string groupDisplay = groupEntity != null && !string.IsNullOrWhiteSpace(groupEntity.Name) ? $"{groupName} - {groupEntity.Name.Trim()}" : groupName;

            // Tìm vật tư chuẩn từ DB (Cấp 2)
            var matEntity = materials.FirstOrDefault(m => m.Code?.Value?.Trim().Equals(col.MatCode, StringComparison.OrdinalIgnoreCase) == true);
            string matCode = matEntity?.Code?.Value?.Trim() ?? col.MatCode;
            string matDisplay = matEntity != null && !string.IsNullOrWhiteSpace(matEntity.Name) ? $"{matCode} - {matEntity.Name.Trim()}" : matCode;

            columnDefs.Add((groupName, matCode, groupDisplay, matDisplay));
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức Lò chợ");

        var dataSourceSheet = workbook.Worksheets.Add("DataSources");
        dataSourceSheet.Hide();

        for (int i = 0; i < groupDisplayList.Count; i++) dataSourceSheet.Cell(i + 1, 20).Value = groupDisplayList[i];
        var groupValidationRange = dataSourceSheet.Range(1, 20, Math.Max(1, groupDisplayList.Count), 20);

        for (int i = 0; i < matDisplayList.Count; i++) dataSourceSheet.Cell(i + 1, 21).Value = matDisplayList[i];
        var nameValidationRange = dataSourceSheet.Range(1, 21, Math.Max(1, matDisplayList.Count), 21);

        string startMonth = list.Any() ? list.Min(x => x.StartMonth).ToString("MM/yyyy") : string.Empty;
        string endMonth = list.Any() ? list.Max(x => x.EndMonth).ToString("MM/yyyy") : string.Empty;

        const int colProcess = 1, colTech = 2, colHardness = 3, colPower = 4, colLongwallParams = 5, colCuttingThickness = 6, startMatrixCol = 7;
        const int headerRow1 = 5, headerRow2 = 6, headerRow3 = 7, startDataRow = 8;

        worksheet.Cell("A1").Value = $"BẢNG ĐƠN GIÁ VÀ ĐỊNH MỨC VẬT LIỆU LÒ CHỢ NĂM {DateTime.Now.Year}";
        worksheet.Range("A1:G1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        worksheet.Cell("A3").Value = "Thời gian bắt đầu:"; worksheet.Cell("B3").Value = startMonth;
        worksheet.Cell("D3").Value = "Thời gian kết thúc:"; worksheet.Cell("E3").Value = endMonth;
        worksheet.Range("A3:E3").Style.Font.SetBold();

        var fixedHeaders = new[] { (colProcess, "Công đoạn sản xuất"), (colTech, "Công nghệ khai thác"), (colHardness, "Hệ số kiên cố (f)"), (colPower, "Công suất (tấn)"), (colLongwallParams, "Thông số lò chợ"), (colCuttingThickness, "Chiều dày lớp khấu (m)") };
        foreach (var (col, title) in fixedHeaders) { var range = worksheet.Range(headerRow1, col, headerRow3, col); range.Merge().Value = title; ApplyHeaderStyle(range); }

        int currentMatrixCol = startMatrixCol;

        foreach (var seamFace in seamFaceNames)
        {
            int faceStartCol = currentMatrixCol;
            var codeRange = worksheet.Range(headerRow1, currentMatrixCol, headerRow3, currentMatrixCol);
            codeRange.Merge().Value = "Mã định mức";
            ApplyHeaderStyle(codeRange);
            worksheet.Column(currentMatrixCol).Width = 14;
            currentMatrixCol++;

            foreach (var colDef in columnDefs)
            {
                var cellLevel1 = worksheet.Cell(headerRow1, currentMatrixCol);
                cellLevel1.Value = colDef.GroupDisplay;
                ApplyHeaderStyle(worksheet.Range(headerRow1, currentMatrixCol, headerRow1, currentMatrixCol));
                if (groupDisplayList.Any()) cellLevel1.CreateDataValidation().List(groupValidationRange);

                var cellLevel2 = worksheet.Cell(headerRow2, currentMatrixCol);
                cellLevel2.Value = colDef.MatDisplay;
                ApplyHeaderStyle(worksheet.Range(headerRow2, currentMatrixCol, headerRow2, currentMatrixCol));
                if (matDisplayList.Any()) cellLevel2.CreateDataValidation().List(nameValidationRange);

                worksheet.Column(currentMatrixCol).Width = 24; // Tăng width lên để chuỗi "Mã - Tên" không bị khuất chữ
                currentMatrixCol++;
            }

            var faceRange = worksheet.Range(headerRow3, faceStartCol + 1, headerRow3, currentMatrixCol - 1);
            faceRange.Merge().Value = $"{seamFace}";
            ApplyHeaderStyle(faceRange);
        }

        var groupedData = list.GroupBy(data => new {
            ProcessName = data.ProductionProcess?.Name?.Trim() ?? string.Empty,
            TechnologyName = data.Technology?.Value?.Trim() ?? string.Empty,
            HardnessName = data.Hardness?.Value?.Trim() ?? string.Empty,
            PowerName = data.Power?.Value?.Trim() ?? string.Empty,
            LongwallParamsName = data.LongwallParameters != null ? $"{data.LongwallParameters.Llc}-{data.LongwallParameters.Lkc}-{data.LongwallParameters.Mk}" : string.Empty,
            CuttingThickness = data.CuttingThickness?.Value?.Trim() ?? string.Empty
        }).OrderBy(g => g.Key.ProcessName).ThenBy(g => g.Key.TechnologyName).ThenBy(g => ExtractLeadingNumber(g.Key.HardnessName)).ToList();

        var rowIndex = startDataRow;

        foreach (var group in groupedData)
        {
            worksheet.Cell(rowIndex, colProcess).Value = group.Key.ProcessName;
            worksheet.Cell(rowIndex, colTech).Value = group.Key.TechnologyName;
            worksheet.Cell(rowIndex, colHardness).Value = group.Key.HardnessName;
            worksheet.Cell(rowIndex, colPower).Value = group.Key.PowerName;
            worksheet.Cell(rowIndex, colLongwallParams).Value = group.Key.LongwallParamsName;
            worksheet.Cell(rowIndex, colCuttingThickness).Value = group.Key.CuttingThickness;

            int colDataIndex = startMatrixCol;

            foreach (var seamFace in seamFaceNames)
            {
                var entitiesForFace = group.Where(x => GetSeamFaceDisplayName(x).Equals(seamFace, StringComparison.OrdinalIgnoreCase)).ToList();
                int codeColIndex = colDataIndex;
                colDataIndex++;

                if (entitiesForFace.Any())
                {
                    var maLoList = entitiesForFace.Select(x => x.Code?.Value?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList();
                    var codeCell = worksheet.Cell(rowIndex, codeColIndex);
                    codeCell.Value = string.Join(", ", maLoList);
                    codeCell.Style.Font.SetBold().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    codeCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");

                    var costMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entity in entitiesForFace)
                    {
                        foreach (var mac in entity.MaterialUnitPriceAssignmentCodes)
                        {
                            var gName = GetAssignmentDisplayName(mac); 
                            if (!string.IsNullOrWhiteSpace(gName))
                            {
                                if (costMap.ContainsKey(gName)) costMap[gName] += mac.Norm;
                                else costMap[gName] = mac.Norm;

                            }
                        }
                    }

                    foreach (var colDef in columnDefs)
                    {
                        // Đối chiếu bằng Mã Thuần nội bộ để đổ tiền chính xác vào ô dưới cột tương ứng
                        if (costMap.TryGetValue(colDef.GroupName, out var amount))
                        {
                            worksheet.Cell(rowIndex, colDataIndex).Value = amount;
                        }
                        colDataIndex++;
                    }
                }
                else
                {
                    colDataIndex += columnDefs.Count;
                }
            }
            rowIndex++;
        }

        var lastDataRow = Math.Max(rowIndex - 1, startDataRow + 100);
        AddDropdownValidation(workbook, worksheet, colProcess, processes.ToList(), lastDataRow, 1, startDataRow);
        AddDropdownValidation(workbook, worksheet, colTech, technologies.ToList(), lastDataRow, 2, startDataRow);
        AddDropdownValidation(workbook, worksheet, colHardness, hardnessOptions, lastDataRow, 3, startDataRow);
        AddDropdownValidation(workbook, worksheet, colPower, powerOptions, lastDataRow, 4, startDataRow);
        AddDropdownValidation(workbook, worksheet, colLongwallParams, longwallParamsOptions, lastDataRow, 5, startDataRow);
        AddDropdownValidation(workbook, worksheet, colCuttingThickness, ctOptions, lastDataRow, 6, startDataRow);

        var fullTableRange = worksheet.Range(headerRow1, colProcess, Math.Max(startDataRow, rowIndex - 1), currentMatrixCol - 1);
        fullTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Columns(colProcess, colCuttingThickness).AdjustToContents();
        worksheet.SheetView.FreezeRows(headerRow3);
        worksheet.SheetView.FreezeColumns(colCuttingThickness);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ApplyHeaderStyle(IXLRange range)
    {
        range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        range.Style.Font.Bold = true;
        range.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
        range.Style.Font.FontColor = XLColor.White;
        range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        range.Style.Alignment.WrapText = true;
    }

    private static string GetSeamFaceDisplayName(LongwallMaterialUnitPriceEntity data) => data.SeamFace?.Value?.Trim() ?? string.Empty;

    private static string GetAssignmentDisplayName(MaterialUnitPriceAssignmentCode item)
    {
        var code = item.AssignmentCode?.Code?.Value?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(code) ? code : (item.AssignmentCode?.Name?.Trim() ?? string.Empty);
    }

    private static string GetAssignmentDisplayName(AssignmentCode assignment)
    {
        var code = assignment.Code?.Value?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(code) ? code : (assignment.Name?.Trim() ?? string.Empty);
    }

    private static void AddDropdownValidation(XLWorkbook workbook, IXLWorksheet worksheet, int targetColumn, List<string> options, int lastDataRow, int sourceColumn, int firstDataRow)
    {
        if (!options.Any()) return;

        var dataSourceSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "DataSources") ?? workbook.Worksheets.Add("DataSources");
        dataSourceSheet.Hide();

        for (var row = 0; row < options.Count; row++)
            dataSourceSheet.Cell(row + 1, sourceColumn).Value = options[row];

        var validation = worksheet.Range(firstDataRow, targetColumn, lastDataRow, targetColumn).CreateDataValidation();
        validation.List(dataSourceSheet.Range(1, sourceColumn, options.Count, sourceColumn));
        validation.InputTitle = "Lựa chọn";
        validation.InputMessage = "Vui lòng chọn từ danh sách.";
        validation.ErrorStyle = XLErrorStyle.Stop;
        validation.ErrorMessage = "Giá trị không hợp lệ!";
    }

    private static double ExtractLeadingNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return double.MaxValue;

        var match = Regex.Match(value, @"\d+([.,]\d+)?");
        if (match.Success)
        {
            if (double.TryParse(match.Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                return val;
        }
        return double.MaxValue;
    }
}