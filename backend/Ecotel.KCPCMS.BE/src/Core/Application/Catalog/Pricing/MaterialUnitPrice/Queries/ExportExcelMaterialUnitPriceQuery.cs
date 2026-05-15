using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;

public record ExportExcelMaterialUnitPriceQuery(
    TunnelExcavationTrimingUnitPriceType Type = TunnelExcavationTrimingUnitPriceType.TunnelExcavation) : IRequest<byte[]>;

public class ExportExcelMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelMaterialUnitPriceQuery, byte[]>
{
    private const string OtherMaterialDisplay = "VTK - Vật tư khác";

    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    private readonly IWriteRepository<SupportStep> _supportStepRepository = unitOfWork.GetRepository<SupportStep>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();

    public async Task<byte[]> Handle(ExportExcelMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var list = await _materialUnitPriceRepository.GetAllAsync(
            predicate: s => s.Type == request.Type,
            include: s => s
                .Include(s => s.ProductionProcess)
                .Include(s => s.Passport)
                .Include(s => s.Hardness)
                .Include(s => s.InsertItem)
                .Include(s => s.SupportStep)
                .Include(s => s.Code)
                .Include(s => s.MaterialUnitPriceAssignmentCodes)
                    .ThenInclude(c => c.AssignmentCode)
                        .ThenInclude(a => a.Code),
            disableTracking: true);

        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(selector: p => $"H/c {p.Name}; {p.Sd}; {p.Sc}", orderBy: p => p.OrderBy(p => p.CreatedOn), disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true);
        var insertItems = await _insertItemRepository.GetAllAsync(selector: i => i.Value, disableTracking: true);
        var supportSteps = await _supportStepRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);
        var assignments = await _assignmentCodeRepository.GetAllAsync(
            include: a => a.Include(x => x.Code),
            disableTracking: true);
        var assignmentOptions = assignments
            .Select(GetAssignmentDisplayName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
        if (!assignmentOptions.Contains(OtherMaterialDisplay, StringComparer.OrdinalIgnoreCase))
        {
            assignmentOptions.Add(OtherMaterialDisplay);
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức vật tư lò đào");

        const int startMonthCol = 1;
        const int endMonthCol = 2;
        const int processCol = 3;
        const int hardnessCol = 4;
        const int insertItemCol = 5;
        const int supportStepCol = 6;
        const int assignmentCol = 7;
        const int passportStartCol = 8;

        var fixedHeaders = new[]
        {
            (startMonthCol, "Thời gian bắt đầu"),
            (endMonthCol, "Thời gian kết thúc"),
            (processCol, "Công đoạn sản xuất"),
            (hardnessCol, "Độ kiên cố than đá"),
            (insertItemCol, "Chèn"),
            (supportStepCol, "Bước chống"),
            (assignmentCol, "Nhóm vật tư, tài sản")
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

        var passportList = passports.ToList();
        var passportColumns = passportList
            .Select((name, index) => new
            {
                name,
                valueCol = passportStartCol + index
            })
            .ToList();

        foreach (var passport in passportColumns)
        {
            var passportRange = worksheet.Range(1, passport.valueCol, 2, passport.valueCol);
            passportRange.Merge();
            passportRange.Value = passport.name;
            passportRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            passportRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            passportRange.Style.Font.Bold = true;
            passportRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            passportRange.Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { passport.valueCol }, passport.name));
        }

        var groupedData = list
            .GroupBy(data => new
            {
                StartMonth = data.StartMonth.ToString("MM/yyyy"),
                EndMonth = data.EndMonth.ToString("MM/yyyy"),
                ProcessName = data.ProductionProcess?.Name?.Trim() ?? string.Empty,
                HardnessName = data.Hardness?.Value?.Trim() ?? string.Empty,
                InsertItemName = data.InsertItem?.Value?.Trim() ?? string.Empty,
                SupportStepName = data.SupportStep?.Value?.Trim() ?? string.Empty
            })
            .OrderBy(group => group.Key.StartMonth)
            .ThenBy(group => group.Key.EndMonth)
            .ThenBy(group => group.Key.ProcessName)
            .ThenBy(group => group.Key.HardnessName)
            .ThenBy(group => group.Key.InsertItemName)
            .ThenBy(group => group.Key.SupportStepName)
            .ToList();

        var blockRanges = new List<(int startRow, int endRow)>();
        var baseRows = new List<int>();

        var rowIndex = 3;
        foreach (var group in groupedData)
        {
            var baseRow = rowIndex;
            worksheet.Cell(baseRow, startMonthCol).Value = group.Key.StartMonth;
            worksheet.Cell(baseRow, endMonthCol).Value = group.Key.EndMonth;
            worksheet.Cell(baseRow, processCol).Value = group.Key.ProcessName;
            worksheet.Cell(baseRow, hardnessCol).Value = group.Key.HardnessName;
            worksheet.Cell(baseRow, insertItemCol).Value = group.Key.InsertItemName;
            worksheet.Cell(baseRow, supportStepCol).Value = group.Key.SupportStepName;

            var passportEntities = group
                .Where(data => !string.IsNullOrWhiteSpace(GetPassportDisplayName(data)))
                .GroupBy(GetPassportDisplayName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            var assignmentRows = BuildAssignmentRows(group, assignmentOptions);
            for (var i = 0; i < assignmentRows.Count; i++)
            {
                var assignmentRow = baseRow + i + 1;
                worksheet.Cell(assignmentRow, assignmentCol).Value = assignmentRows[i];
            }
            baseRows.Add(baseRow);

            foreach (var passport in passportColumns)
            {
                if (!passportEntities.TryGetValue(passport.name, out var entity))
                {
                    continue;
                }

                worksheet.Cell(baseRow, passport.valueCol).Value = entity.Code?.Value ?? string.Empty;
                var costMap = BuildCostMap(entity);

                for (var i = 0; i < assignmentRows.Count; i++)
                {
                    var assignmentDisplay = assignmentRows[i];
                    if (string.IsNullOrWhiteSpace(assignmentDisplay))
                    {
                        continue;
                    }

                    if (costMap.TryGetValue(assignmentDisplay, out var amount))
                    {
                        worksheet.Cell(baseRow + i + 1, passport.valueCol).Value = amount;
                    }
                }
            }

            blockRanges.Add((baseRow, baseRow + assignmentRows.Count));
            rowIndex += assignmentRows.Count + 1;
        }

        var lastDataRow = Math.Max(rowIndex - 1, 100);
        AddDropdownValidation(workbook, worksheet, processCol, processes.ToList(), lastDataRow, 1);
        AddDropdownValidation(workbook, worksheet, hardnessCol, hardnesses.ToList(), lastDataRow, 2);
        AddDropdownValidation(workbook, worksheet, insertItemCol, insertItems.ToList(), lastDataRow, 3);
        AddDropdownValidation(workbook, worksheet, supportStepCol, supportSteps.ToList(), lastDataRow, 4);
        AddDropdownValidation(workbook, worksheet, assignmentCol, assignmentOptions, lastDataRow, 5);

        var lastHeaderCol = Math.Max(supportStepCol, passportStartCol + passportList.Count - 1);

        foreach (var (columns, text) in headerWidthInstructions)
        {
            ApplyColumnWidthForHeader(worksheet, columns, text);
        }

        var fullTableRange = worksheet.Range(1, startMonthCol, lastDataRow, lastHeaderCol);
        fullTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        fullTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Improve readability by styling row types and separating each block.
        foreach (var baseRow in baseRows)
        {
            worksheet.Range(baseRow, assignmentCol, baseRow, lastHeaderCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#E9F1FB");
            worksheet.Cell(baseRow, assignmentCol).Style.Font.Bold = true;
        }

        worksheet.SheetView.FreezeRows(2);
        worksheet.SheetView.FreezeColumns(assignmentCol);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void AddDropdownValidation(
        XLWorkbook workbook,
        IXLWorksheet worksheet,
        int targetColumn,
        List<string> options,
        int lastDataRow,
        int sourceColumn)
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

        for (int row = 0; row < options.Count; row++)
        {
            dataSourceSheet.Cell(row + 1, sourceColumn).Value = options[row];
        }

        var sourceRange = dataSourceSheet.Range(1, sourceColumn, options.Count, sourceColumn);
        var targetRange = worksheet.Range(3, targetColumn, lastDataRow, targetColumn);
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

    private static string GetPassportDisplayName(TunnelExcavationMaterialUnitPrice data)
    {
        if (data.Passport == null)
        {
            return string.Empty;
        }

        return $"H/c {data.Passport.Name}; {data.Passport.Sd}; {data.Passport.Sc}";
    }

    private static List<string> BuildAssignmentRows(
        IEnumerable<TunnelExcavationMaterialUnitPrice> entities,
        IReadOnlyList<string> assignmentOptions)
    {
        var allAssignments = entities
            .SelectMany(entity => BuildCostMap(entity).Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!allAssignments.Any())
        {
            return [string.Empty, string.Empty, string.Empty, OtherMaterialDisplay];
        }

        var optionIndex = assignmentOptions
            .Select((value, index) => new { value, index })
            .ToDictionary(x => x.value, x => x.index, StringComparer.OrdinalIgnoreCase);

        return allAssignments
            .OrderBy(value => optionIndex.TryGetValue(value, out var index) ? index : int.MaxValue)
            .ThenBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, double> BuildCostMap(TunnelExcavationMaterialUnitPrice entity)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in entity.MaterialUnitPriceAssignmentCodes)
        {
            var assignmentDisplay = GetAssignmentDisplayName(item);
            if (string.IsNullOrWhiteSpace(assignmentDisplay))
            {
                continue;
            }

            map[assignmentDisplay] = item.TotalPrice;
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
}
