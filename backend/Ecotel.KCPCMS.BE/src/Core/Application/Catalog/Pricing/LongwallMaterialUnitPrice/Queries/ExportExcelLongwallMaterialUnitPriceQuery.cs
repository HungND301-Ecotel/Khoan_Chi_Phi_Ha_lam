using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var longwallParametersData = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var longwallParameters = longwallParametersData.Select(l => $"{l.Llc}-{l.Lkc}-{l.Mk}").ToList();
        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(selector: c => c.Value, disableTracking: true);
        var seamFaceEntities = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);
        var hardnessOptions = (await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        var powerOptions = (await _powerRepository.GetAllAsync(selector: p => p.Value, disableTracking: true))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        hardnessOptions.Insert(0, string.Empty);
        powerOptions.Insert(0, string.Empty);
        var assignments = await _assignmentCodeRepository.GetAllAsync(
            include: a => a.Include(x => x.Code),
            disableTracking: true);
        var materials = await _materialRepository.GetAllAsync(
            include: m => m.Include(x => x.Code),
            disableTracking: true);
        var assignmentOptions = assignments
            .Select(GetAssignmentDisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        var materialOptions = materials
            .Select(GetMaterialDisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        if (!assignmentOptions.Contains(OtherMaterialDisplay, StringComparer.OrdinalIgnoreCase))
        {
            assignmentOptions.Add(OtherMaterialDisplay);
        }
        if (!materialOptions.Contains(OtherMaterialDisplay, StringComparer.OrdinalIgnoreCase))
        {
            materialOptions.Add(OtherMaterialDisplay);
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức vật tư lò chợ");

        const int startMonthCol = 1;
        const int endMonthCol = 2;
        const int processCol = 3;
        const int technologyCol = 4;
        const int hardnessCol = 5;
        const int powerCol = 6;
        const int longwallParametersCol = 7;
        const int cuttingThicknessCol = 8;
        const int assignmentCol = 9;
        const int materialCol = 10;
        const int seamFaceStartCol = 11;

        var fixedHeaders = new[]
        {
            (startMonthCol, "Thời gian bắt đầu"),
            (endMonthCol, "Thời gian kết thúc"),
            (processCol, "Công đoạn sản xuất"),
            (technologyCol, "Công nghệ khai thác"),
            (hardnessCol, "Độ kiên cố than đá (f)"),
            (powerCol, "Công suất"),
            (longwallParametersCol, "Thông số lò chợ"),
            (cuttingThicknessCol, "Chiều dày lớp khấu"),
            (assignmentCol, "Nhóm vật tư, tài sản"),
            (materialCol, "Vật tư tài sản")
        };

        var headerWidthInstructions = new List<(int[] columns, string headerText)>();
        foreach (var (col, title) in fixedHeaders)
        {
            var range = worksheet.Range(1, col, 2, col);
            range.Merge();
            range.Value = title;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            range.Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { col }, title));
        }

        var seamFaceNames = seamFaceEntities
            .Select(s => s.Value)
            .OrderBy(v => ExtractLeadingNumber(v))   // sort số trước
            .ThenBy(v => v, StringComparer.OrdinalIgnoreCase) // fallback text
            .ToList(); var seamFaceColumns = seamFaceNames
            .Select((name, index) => new
            {
                name,
                valueCol = seamFaceStartCol + index
            })
            .ToList();

        foreach (var seamFace in seamFaceColumns)
        {
            var faceRange = worksheet.Range(1, seamFace.valueCol, 2, seamFace.valueCol);
            faceRange.Merge();
            faceRange.Value = seamFace.name;
            faceRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            faceRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            faceRange.Style.Font.Bold = true;
            faceRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            faceRange.Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { seamFace.valueCol }, seamFace.name));
        }

        var groupedData = list
            .GroupBy(data => new
            {
                StartMonth = data.StartMonth.ToString("MM/yyyy"),
                EndMonth = data.EndMonth.ToString("MM/yyyy"),
                ProcessName = data.ProductionProcess?.Name?.Trim() ?? string.Empty,
                TechnologyName = data.Technology?.Value?.Trim() ?? string.Empty,
                HardnessName = data.Hardness?.Value?.Trim() ?? string.Empty,
                PowerName = data.Power?.Value?.Trim() ?? string.Empty,
                LongwallParametersName = data.LongwallParameters != null ? $"{data.LongwallParameters.Llc}-{data.LongwallParameters.Lkc}-{data.LongwallParameters.Mk}" : string.Empty,
                CuttingThicknessName = data.CuttingThickness?.Value?.Trim() ?? string.Empty
            })
            .OrderBy(group => group.Key.StartMonth)
            .ThenBy(group => group.Key.EndMonth)
            .ThenBy(group => group.Key.ProcessName)
            .ThenBy(group => group.Key.TechnologyName)
            .ThenBy(group => group.Key.HardnessName)
            .ThenBy(group => group.Key.PowerName)
            .ThenBy(group => group.Key.LongwallParametersName)
            .ThenBy(group => group.Key.CuttingThicknessName)
            .ToList();

        var rowIndex = 3;
        var baseRows = new List<int>();
        foreach (var group in groupedData)
        {
            var baseRow = rowIndex;
            worksheet.Cell(baseRow, startMonthCol).Value = group.Key.StartMonth;
            worksheet.Cell(baseRow, endMonthCol).Value = group.Key.EndMonth;
            worksheet.Cell(baseRow, processCol).Value = group.Key.ProcessName;
            worksheet.Cell(baseRow, technologyCol).Value = group.Key.TechnologyName;
            worksheet.Cell(baseRow, hardnessCol).Value = group.Key.HardnessName;
            worksheet.Cell(baseRow, powerCol).Value = group.Key.PowerName;
            worksheet.Cell(baseRow, longwallParametersCol).Value = group.Key.LongwallParametersName;
            worksheet.Cell(baseRow, cuttingThicknessCol).Value = group.Key.CuttingThicknessName;
            baseRows.Add(baseRow);

            var seamFaceData = group
                .Where(data => !string.IsNullOrWhiteSpace(GetSeamFaceDisplayName(data)))
                .GroupBy(GetSeamFaceDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var detailRows = BuildDetailRows(group, assignmentOptions);
            for (var i = 0; i < detailRows.Count; i++)
            {
                var detailRow = baseRow + i + 1;
                worksheet.Cell(detailRow, assignmentCol).Value = detailRows[i].AssignmentDisplay;
                worksheet.Cell(detailRow, materialCol).Value = detailRows[i].MaterialDisplay;
            }

            foreach (var seamFace in seamFaceColumns)
            {
                if (!seamFaceData.TryGetValue(seamFace.name, out var entity))
                {
                    continue;
                }

                worksheet.Cell(baseRow, seamFace.valueCol).Value = entity.Code?.Value ?? string.Empty;
                var costMap = BuildCostMap(entity);

                for (var i = 0; i < detailRows.Count; i++)
                {
                    var detailRow = detailRows[i];
                    if (string.IsNullOrWhiteSpace(detailRow.Key))
                    {
                        continue;
                    }

                    if (costMap.TryGetValue(detailRow.Key, out var amount))
                    {
                        worksheet.Cell(baseRow + i + 1, seamFace.valueCol).Value = amount;
                    }
                }
            }

            rowIndex += detailRows.Count + 1;
        }

        var lastDataRow = Math.Max(rowIndex - 1, 100);
        AddDropdownValidation(workbook, worksheet, processCol, processes.ToList(), lastDataRow, 1, 3);
        AddDropdownValidation(workbook, worksheet, technologyCol, technologies.ToList(), lastDataRow, 2, 3);
        AddDropdownValidation(workbook, worksheet, hardnessCol, hardnessOptions, lastDataRow, 3, 3);
        AddDropdownValidation(workbook, worksheet, powerCol, powerOptions, lastDataRow, 4, 3);
        AddDropdownValidation(workbook, worksheet, longwallParametersCol, longwallParameters, lastDataRow, 5, 3);
        AddDropdownValidation(workbook, worksheet, cuttingThicknessCol, cuttingThicknesses.ToList(), lastDataRow, 6, 3);
        AddDropdownValidation(workbook, worksheet, assignmentCol, assignmentOptions, lastDataRow, 7, 3);
        AddDropdownValidation(workbook, worksheet, materialCol, materialOptions, lastDataRow, 8, 3);

        var lastHeaderCol = Math.Max(materialCol, seamFaceStartCol + seamFaceNames.Count - 1);
        foreach (var (columns, text) in headerWidthInstructions)
        {
            ApplyColumnWidthForHeader(worksheet, columns, text);
        }

        var fullTableRange = worksheet.Range(1, startMonthCol, lastDataRow, lastHeaderCol);
        fullTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        foreach (var baseRow in baseRows)
        {
            worksheet.Range(baseRow, startMonthCol, baseRow, lastHeaderCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#E9F1FB");
            worksheet.Cell(baseRow, assignmentCol).Style.Font.Bold = true;
        }

        worksheet.SheetView.FreezeRows(2);
        worksheet.SheetView.FreezeColumns(materialCol);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetSeamFaceDisplayName(LongwallMaterialUnitPriceEntity data)
    {
        return data.SeamFace?.Value?.Trim() ?? string.Empty;
    }

    private static List<ExportDetailRow> BuildDetailRows(
        IEnumerable<LongwallMaterialUnitPriceEntity> entities,
        IReadOnlyList<string> assignmentOptions)
    {
        var allRows = entities
            .SelectMany(entity => BuildCostMap(entity).Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!allRows.Any())
        {
            return
            [
                new ExportDetailRow(string.Empty, string.Empty, string.Empty),
                new ExportDetailRow(string.Empty, string.Empty, string.Empty),
                new ExportDetailRow(string.Empty, string.Empty, string.Empty),
                new ExportDetailRow(OtherMaterialDisplay, OtherMaterialDisplay, BuildDetailKey(OtherMaterialDisplay, OtherMaterialDisplay))
            ];
        }

        var optionIndex = assignmentOptions
            .Select((value, index) => new { value, index })
            .ToDictionary(x => x.value, x => x.index, StringComparer.OrdinalIgnoreCase);

        return allRows
            .Select(ParseDetailKey)
            .OrderBy(value => optionIndex.TryGetValue(value.AssignmentDisplay, out var index) ? index : int.MaxValue)
            .ThenBy(value => value.AssignmentDisplay, StringComparer.OrdinalIgnoreCase)
            .ThenBy(value => value.MaterialDisplay, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, double> BuildCostMap(LongwallMaterialUnitPriceEntity entity)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in entity.MaterialUnitPriceAssignmentCodes)
        {
            var assignmentDisplay = GetAssignmentDisplayName(item);
            var materialDisplay = GetMaterialDisplayName(item.Material);
            if (string.IsNullOrWhiteSpace(assignmentDisplay) || string.IsNullOrWhiteSpace(materialDisplay))
            {
                continue;
            }

            map[BuildDetailKey(assignmentDisplay, materialDisplay)] = item.Norm;
        }

        if (entity.OtherMaterialvalue > 0)
        {
            map[BuildDetailKey(OtherMaterialDisplay, OtherMaterialDisplay)] = entity.OtherMaterialvalue;
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

    private static string GetMaterialDisplayName(Domain.Entities.Index.Material? material)
    {
        if (material == null)
        {
            return string.Empty;
        }

        var code = material.Code?.Value?.Trim() ?? string.Empty;
        var name = material.Name?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{code} - {name}";
        }

        return !string.IsNullOrWhiteSpace(code) ? code : name;
    }

    private static string BuildDetailKey(string assignmentDisplay, string materialDisplay)
        => $"{assignmentDisplay}|||{materialDisplay}";

    private static ExportDetailRow ParseDetailKey(string key)
    {
        var parts = key.Split("|||", 2, StringSplitOptions.None);
        var assignmentDisplay = parts.ElementAtOrDefault(0) ?? string.Empty;
        var materialDisplay = parts.ElementAtOrDefault(1) ?? string.Empty;
        return new ExportDetailRow(assignmentDisplay, materialDisplay, key);
    }

    private static string GetAssignmentDisplayName(AssignmentCode assignment)
    {
        var code = assignment.Code?.Value?.Trim() ?? string.Empty;
        var name = assignment.Name?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{code} - {name}";
        }

        return !string.IsNullOrWhiteSpace(code) ? code : name;
    }

    private static void AddDropdownValidation(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        int targetColumn,
        List<string> options,
        int lastDataRow,
        int sourceColumn,
        int firstDataRow)
    {
        if (!options.Any())
        {
            return;
        }

        var dataSourceSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "DataSources");
        if (dataSourceSheet == null)
        {
            dataSourceSheet = workbook.Worksheets.Add("DataSources");
            dataSourceSheet.Hide();
        }

        for (var row = 0; row < options.Count; row++)
        {
            dataSourceSheet.Cell(row + 1, sourceColumn).Value = options[row];
        }

        var sourceRange = dataSourceSheet.Range(1, sourceColumn, options.Count, sourceColumn);
        var targetRange = worksheet.Range(firstDataRow, targetColumn, lastDataRow, targetColumn);
        var validation = targetRange.CreateDataValidation();
        validation.List(sourceRange);
        validation.InputTitle = "Lựa chọn";
        validation.InputMessage = "Vui lòng chọn từ danh sách.";
        validation.ErrorStyle = XLErrorStyle.Stop;
        validation.ErrorMessage = "Giá trị không hợp lệ!";
    }

    private static void ApplyColumnWidthForHeader(IXLWorksheet worksheet, IEnumerable<int> columns, string headerText)
    {
        if (string.IsNullOrWhiteSpace(headerText))
        {
            return;
        }

        var columnIndexes = columns.ToArray();
        if (columnIndexes.Length == 0)
        {
            return;
        }

        var requiredWidth = Math.Max(headerText.Length + 2, 5);
        var perColumnWidth = Math.Max(Math.Ceiling(requiredWidth / (double)columnIndexes.Length), 5);

        foreach (var columnIndex in columnIndexes)
        {
            worksheet.Column(columnIndex).Width = perColumnWidth;
        }
    }

    private static double ExtractLeadingNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return double.MaxValue;
        }

        // Tìm số đầu tiên trong chuỗi, ví dụ: "M =12m" → 12, "M =9m" → 9
        var match = System.Text.RegularExpressions.Regex.Match(value, @"\d+(\.\d+)?");
        return match.Success ? double.Parse(match.Value, System.Globalization.CultureInfo.InvariantCulture) : double.MaxValue;
    }

    private sealed record ExportDetailRow(
        string AssignmentDisplay,
        string MaterialDisplay,
        string Key);
}
