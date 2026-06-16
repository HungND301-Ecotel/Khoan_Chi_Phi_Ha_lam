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
    private const string OtherMaterialDisplay = "VTK - Vật tư khác";

    private readonly IWriteRepository<LongwallMaterialUnitPriceEntity> _materialUnitPriceRepository = unitOfWork.GetRepository<LongwallMaterialUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Power> _powerRepository = unitOfWork.GetRepository<Power>();

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
                        .ThenInclude(a => a.Code),
            disableTracking: true);

        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);
        var hardnessOptions = (await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => ExtractLeadingNumber(x)).ToList();
        var powerOptions = (await _powerRepository.GetAllAsync(selector: p => p.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(selector: c => c.Value, disableTracking: true);
        var ctOptions = cuttingThicknesses.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => ExtractLeadingNumber(x)).ToList();

        var longwallParams = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var llcOptions = longwallParams.Select(l => l.Llc.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => ExtractLeadingNumber(x)).ToList();
        var lkcOptions = longwallParams.Select(l => l.Lkc.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => ExtractLeadingNumber(x)).ToList();
        var mkOptions = longwallParams.Select(l => l.Mk.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => ExtractLeadingNumber(x)).ToList();

        var seamFaceEntities = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var seamFaceNames = seamFaceEntities.Select(s => s.Value)
            .OrderBy(v => ExtractLeadingNumber(v)).ThenBy(v => v, StringComparer.OrdinalIgnoreCase).ToList();

        // danh sách dropdown cho header
        var assignmentOptions = list
            .SelectMany(entity => BuildCostMap(entity).Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList(); // Tên vật tư (Cấp 2)

        var groupOptions = assignmentOptions
            .Select(x => x.Contains(" - ") ? x.Split(" - ")[0].Trim() : x)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList(); // Nhóm vật tư (Cấp 1)

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức Lò chợ");

        var dataSourceSheet = workbook.Worksheets.Add("DataSources");
        dataSourceSheet.Hide();

        for (int i = 0; i < groupOptions.Count; i++)
        {
            dataSourceSheet.Cell(i + 1, 20).Value = groupOptions[i];
        }

        var groupValidationRange = dataSourceSheet.Range(1, 20, Math.Max(1, groupOptions.Count), 20);

        for (int i = 0; i < assignmentOptions.Count; i++)
        {
            dataSourceSheet.Cell(i + 1, 21).Value = assignmentOptions[i];
        }

        var nameValidationRange = dataSourceSheet.Range(1, 21, Math.Max(1, assignmentOptions.Count), 21);

        var firstRecord = list.FirstOrDefault();
        string startMonth = firstRecord != null ? firstRecord.StartMonth.ToString("MM/yyyy") : string.Empty;
        string endMonth = firstRecord != null ? firstRecord.EndMonth.ToString("MM/yyyy") : string.Empty;

        // tọa dộ cột
        const int colSTT = 1;
        const int colProcess = 2;
        const int colTech = 3;
        const int colHardness = 4;
        const int colPower = 5;
        const int colCuttingThickness = 6;
        const int colLlc = 7;
        const int colLkc = 8;
        const int colMk = 9;
        const int startMatrixCol = 10;

        const int headerRow1 = 5; // Cấp 1: Mã Nhóm vật tư
        const int headerRow2 = 6; // Cấp 2: Tên Vật tư chi tiết
        const int headerRow3 = 7; // Cấp 3: Gộp Mặt vỉa
        const int startDataRow = 8;

        // VẼ THÔNG TIN CHUNG
        worksheet.Cell("A1").Value = $"BẢNG ĐƠN GIÁ VÀ ĐỊNH MỨC VẬT LIỆU LÒ CHỢ NĂM {DateTime.Now.Year}";
        worksheet.Range("A1:I1").Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        worksheet.Cell("A3").Value = "Thời gian bắt đầu:";
        worksheet.Cell("B3").Value = startMonth;
        worksheet.Cell("D3").Value = "Thời gian kết thúc:";
        worksheet.Cell("E3").Value = endMonth;
        worksheet.Range("A3:E3").Style.Font.SetBold();

        var fixedHeaders = new[]
        {
            (colSTT, "STT"),
            (colProcess, "Công đoạn sx"),
            (colTech, "Công nghệ khai thác"),
            (colHardness, "Hệ số kiên cố (f)"),
            (colPower, "Công suất (Tấn)"),
            (colCuttingThickness, "Chiều dày M(m)")
        };

        foreach (var (col, title) in fixedHeaders)
        {
            var range = worksheet.Range(headerRow1, col, headerRow3, col);
            range.Merge().Value = title;
            ApplyHeaderStyle(range);
        }

        var paramRange = worksheet.Range(headerRow1, colLlc, headerRow1, colMk);
        paramRange.Merge().Value = "Thông số lò chợ";
        ApplyHeaderStyle(paramRange);

        worksheet.Cell(headerRow2, colLlc).Value = "Llc(m)";
        worksheet.Range(headerRow2, colLlc, headerRow3, colLlc).Merge();

        worksheet.Cell(headerRow2, colLkc).Value = "Lkc(m)";
        worksheet.Range(headerRow2, colLkc, headerRow3, colLkc).Merge();

        worksheet.Cell(headerRow2, colMk).Value = "Mk(m)";
        worksheet.Range(headerRow2, colMk, headerRow3, colMk).Merge();

        ApplyHeaderStyle(worksheet.Range(headerRow2, colLlc, headerRow3, colMk));

        int currentMatrixCol = startMatrixCol;

        foreach (var seamFace in seamFaceNames)
        {
            var codeRange = worksheet.Range(headerRow1, currentMatrixCol, headerRow3, currentMatrixCol);
            codeRange.Merge().Value = "Mã định mức";
            ApplyHeaderStyle(codeRange);
            worksheet.Column(currentMatrixCol).Width = 14;
            currentMatrixCol++;

            if (assignmentOptions.Any())
            {
                int startMatCol = currentMatrixCol;

                foreach (var material in assignmentOptions)
                {
                    string level1Group = material;
                    string level2Name = material;

                    if (material.Contains(" - "))
                    {
                        level1Group = material.Split(" - ")[0].Trim();
                    }

                    //Nhóm vật tư 
                    var cellLevel1 = worksheet.Cell(headerRow1, currentMatrixCol);
                    cellLevel1.Value = level1Group;
                    ApplyHeaderStyle(worksheet.Range(headerRow1, currentMatrixCol, headerRow1, currentMatrixCol));
                    if (groupOptions.Any())
                    {
                        cellLevel1.CreateDataValidation().List(groupValidationRange);
                    }

                    // Tên chi tiết 
                    var cellLevel2 = worksheet.Cell(headerRow2, currentMatrixCol);
                    cellLevel2.Value = level2Name;
                    ApplyHeaderStyle(worksheet.Range(headerRow2, currentMatrixCol, headerRow2, currentMatrixCol));
                    if (assignmentOptions.Any())
                    {
                        cellLevel2.CreateDataValidation().List(nameValidationRange);
                    }

                    worksheet.Column(currentMatrixCol).Width = 13;
                    currentMatrixCol++;
                }

                int endMatCol = currentMatrixCol - 1;

                // Tên Mặt vỉa
                var faceRange = worksheet.Range(headerRow3, startMatCol, headerRow3, endMatCol);
                faceRange.Merge().Value = $"{seamFace}";
                ApplyHeaderStyle(faceRange);
            }
        }

        var lastHeaderCol = Math.Max(colMk, currentMatrixCol - 1);

        var groupedData = list
            .GroupBy(data => new
            {
                ProcessName = data.ProductionProcess?.Name?.Trim() ?? string.Empty,
                TechnologyName = data.Technology?.Value?.Trim() ?? string.Empty,
                HardnessName = data.Hardness?.Value?.Trim() ?? string.Empty,
                PowerName = data.Power?.Value?.Trim() ?? string.Empty,
                CuttingThickness = data.CuttingThickness?.Value?.Trim() ?? string.Empty,
                Llc = data.LongwallParameters?.Llc?.ToString() ?? string.Empty,
                Lkc = data.LongwallParameters?.Lkc?.ToString() ?? string.Empty,
                Mk = data.LongwallParameters?.Mk?.ToString() ?? string.Empty
            })
            .OrderBy(g => g.Key.ProcessName)
            .ThenBy(g => g.Key.TechnologyName)
            .ThenBy(g => ExtractHardnessOrder(g.Key.HardnessName))
            .ThenBy(g => ExtractLeadingNumber(g.Key.Llc))
            .ToList();

        var rowIndex = startDataRow;
        int sttIndex = 1;

        foreach (var group in groupedData)
        {
            worksheet.Cell(rowIndex, colSTT).Value = sttIndex;
            worksheet.Cell(rowIndex, colProcess).Value = group.Key.ProcessName;
            worksheet.Cell(rowIndex, colTech).Value = group.Key.TechnologyName;
            worksheet.Cell(rowIndex, colHardness).Value = group.Key.HardnessName;
            worksheet.Cell(rowIndex, colPower).Value = group.Key.PowerName;
            worksheet.Cell(rowIndex, colCuttingThickness).Value = group.Key.CuttingThickness;
            worksheet.Cell(rowIndex, colLlc).Value = group.Key.Llc;
            worksheet.Cell(rowIndex, colLkc).Value = group.Key.Lkc;
            worksheet.Cell(rowIndex, colMk).Value = group.Key.Mk;

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
                    codeCell.Style.Font.SetBold();
                    codeCell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    codeCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");

                    var costMap = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entity in entitiesForFace)
                    {
                        var map = BuildCostMap(entity);
                        foreach (var kvp in map)
                        {
                            costMap[kvp.Key] = kvp.Value;
                        }
                    }

                    foreach (var material in assignmentOptions)
                    {
                        if (costMap.TryGetValue(material, out var amount))
                        {
                            worksheet.Cell(rowIndex, colDataIndex).Value = amount;
                        }
                        colDataIndex++;
                    }
                }
                else
                {
                    colDataIndex += assignmentOptions.Count;
                }
            }

            rowIndex++;
            sttIndex++;
        }

        var lastDataRow = Math.Max(rowIndex - 1, startDataRow + 100);

        AddDropdownValidation(workbook, worksheet, colProcess, processes.ToList(), lastDataRow, 1, startDataRow);
        AddDropdownValidation(workbook, worksheet, colTech, technologies.ToList(), lastDataRow, 2, startDataRow);
        AddDropdownValidation(workbook, worksheet, colHardness, hardnessOptions, lastDataRow, 3, startDataRow);
        AddDropdownValidation(workbook, worksheet, colPower, powerOptions, lastDataRow, 4, startDataRow);
        AddDropdownValidation(workbook, worksheet, colCuttingThickness, ctOptions, lastDataRow, 5, startDataRow);
        AddDropdownValidation(workbook, worksheet, colLlc, llcOptions, lastDataRow, 6, startDataRow);
        AddDropdownValidation(workbook, worksheet, colLkc, lkcOptions, lastDataRow, 7, startDataRow);
        AddDropdownValidation(workbook, worksheet, colMk, mkOptions, lastDataRow, 8, startDataRow);

        var fullTableRange = worksheet.Range(headerRow1, colSTT, Math.Max(startDataRow, rowIndex - 1), lastHeaderCol);
        fullTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        worksheet.Columns(colSTT, colMk).AdjustToContents();
        worksheet.SheetView.FreezeRows(headerRow3);
        worksheet.SheetView.FreezeColumns(colMk);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // --- HELPERS ---
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

    private static Dictionary<string, double> BuildCostMap(LongwallMaterialUnitPriceEntity entity)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in entity.MaterialUnitPriceAssignmentCodes)
        {
            var assignmentDisplay = GetAssignmentDisplayName(item);
            if (!string.IsNullOrWhiteSpace(assignmentDisplay))
            {
                map[assignmentDisplay] = item.TotalPrice;
            }
        }
        if (entity.OtherMaterialvalue > 0)
        {
            map[OtherMaterialDisplay] = entity.OtherMaterialvalue;
        }

        return map;
    }

    private static string GetAssignmentDisplayName(MaterialUnitPriceAssignmentCode item)
    {
        var code = item.AssignmentCode?.Code?.Value?.Trim() ?? string.Empty;
        var name = item.AssignmentCode?.Name?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{code} - {name}";
        }

        return !string.IsNullOrWhiteSpace(code) ? code : name;
    }

    private static void AddDropdownValidation(XLWorkbook workbook, IXLWorksheet worksheet, int targetColumn, List<string> options, int lastDataRow, int sourceColumn, int firstDataRow)
    {
        if (!options.Any())
        {
            return;
        }

        var dataSourceSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "DataSources") ?? workbook.Worksheets.Add("DataSources");
        dataSourceSheet.Hide();

        for (var row = 0; row < options.Count; row++)
        {
            dataSourceSheet.Cell(row + 1, sourceColumn).Value = options[row];
        }

        var validation = worksheet.Range(firstDataRow, targetColumn, lastDataRow, targetColumn).CreateDataValidation();
        validation.List(dataSourceSheet.Range(1, sourceColumn, options.Count, sourceColumn));
        validation.InputTitle = "Lựa chọn";
        validation.InputMessage = "Vui lòng chọn từ danh sách.";
        validation.ErrorStyle = XLErrorStyle.Stop;
        validation.ErrorMessage = "Giá trị không hợp lệ!";
    }

    private static double ExtractLeadingNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.MaxValue;
        }

        var match = Regex.Match(value, @"\d+([.,]\d+)?");
        if (match.Success)
        {
            if (double.TryParse(match.Value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
            {
                return val;
            }
        }
        return double.MaxValue;
    }

    private static double ExtractHardnessOrder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.MaxValue;
        }

        var matches = Regex.Matches(value, @"\d+([.,]\d+)?");
        if (matches.Count == 0)
        {
            return double.MaxValue;
        }

        // Lấy số cuối cùng (upper bound)
        var lastMatch = matches[matches.Count - 1];
        if (double.TryParse(lastMatch.Value.Replace(",", "."),
            NumberStyles.Any, CultureInfo.InvariantCulture, out double upper))
        {
            // Nếu có 2 số, lấy thêm lower bound để sort thứ cấp
            double lower = 0;
            if (matches.Count >= 2)
            {
                double.TryParse(matches[0].Value.Replace(",", "."),
                    NumberStyles.Any, CultureInfo.InvariantCulture, out lower);
            }
            return upper * 1000 + lower;
        }

        return double.MaxValue;
    }
}