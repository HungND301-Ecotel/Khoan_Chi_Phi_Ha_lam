using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Queries;

public record ExportExcelMaterialUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelMaterialUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    private readonly IWriteRepository<SupportStep> _supportStepRepository = unitOfWork.GetRepository<SupportStep>();

    public async Task<byte[]> Handle(ExportExcelMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var list = await _materialUnitPriceRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProductionProcess)
                .Include(s => s.Passport)
                .Include(s => s.Hardness)
                .Include(s => s.InsertItem)
                .Include(s => s.SupportStep)
                .Include(s => s.Code),
            disableTracking: true);

        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(selector: p => $"H/c {p.Name}; {p.Sd}; {p.Sc}", disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true);
        var insertItems = await _insertItemRepository.GetAllAsync(selector: i => i.Value, disableTracking: true);
        var supportSteps = await _supportStepRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức vật tư lò đào");

        const int startMonthCol = 1;
        const int endMonthCol = 2;
        const int processCol = 3;
        const int hardnessCol = 4;
        const int insertItemCol = 5;
        const int supportStepCol = 6;
        const int passportStartCol = 7;

        var fixedHeaders = new[]
        {
            (startMonthCol, "Thời gian bắt đầu"),
            (endMonthCol, "Thời gian kết thúc"),
            (processCol, "Công đoạn sản xuất"),
            (hardnessCol, "Độ kiên cố than đá"),
            (insertItemCol, "Chèn"),
            (supportStepCol, "Bước chống")
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
                dmCol = passportStartCol + (index * 2),
                ttCol = passportStartCol + (index * 2) + 1
            })
            .ToList();

        foreach (var passport in passportColumns)
        {
            var passportRange = worksheet.Range(1, passport.dmCol, 1, passport.ttCol);
            passportRange.Merge();
            passportRange.Value = passport.name;
            passportRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            passportRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            passportRange.Style.Font.Bold = true;
            passportRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            passportRange.Style.Font.FontColor = XLColor.White;

            worksheet.Cell(2, passport.dmCol).Value = "Mã định mức vật liệu";
            worksheet.Cell(2, passport.ttCol).Value = "TT";
            worksheet.Cell(2, passport.dmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, passport.ttCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(2, passport.dmCol).Style.Font.Bold = true;
            worksheet.Cell(2, passport.ttCol).Style.Font.Bold = true;
            worksheet.Cell(2, passport.dmCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            worksheet.Cell(2, passport.ttCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            worksheet.Cell(2, passport.dmCol).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(2, passport.ttCol).Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { passport.dmCol, passport.ttCol }, passport.name));
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

        var rowIndex = 3;
        foreach (var group in groupedData)
        {
            worksheet.Cell(rowIndex, startMonthCol).Value = group.Key.StartMonth;
            worksheet.Cell(rowIndex, endMonthCol).Value = group.Key.EndMonth;
            worksheet.Cell(rowIndex, processCol).Value = group.Key.ProcessName;
            worksheet.Cell(rowIndex, hardnessCol).Value = group.Key.HardnessName;
            worksheet.Cell(rowIndex, insertItemCol).Value = group.Key.InsertItemName;
            worksheet.Cell(rowIndex, supportStepCol).Value = group.Key.SupportStepName;

            var passportEntities = group
                .Where(data => !string.IsNullOrWhiteSpace(GetPassportDisplayName(data)))
                .ToDictionary(data => GetPassportDisplayName(data), data => data);

            foreach (var passport in passportColumns)
            {
                if (passportEntities.TryGetValue(passport.name, out var entity))
                {
                    worksheet.Cell(rowIndex, passport.dmCol).Value = entity.Code?.Value ?? string.Empty;
                    worksheet.Cell(rowIndex, passport.ttCol).Value = entity.TotalPrice;
                }
            }

            rowIndex++;
        }

        var lastDataRow = Math.Max(rowIndex - 1, 100);
        AddDropdownValidation(workbook, worksheet, processCol, processes.ToList(), lastDataRow, 1);
        AddDropdownValidation(workbook, worksheet, hardnessCol, hardnesses.ToList(), lastDataRow, 2);
        AddDropdownValidation(workbook, worksheet, insertItemCol, insertItems.ToList(), lastDataRow, 3);
        AddDropdownValidation(workbook, worksheet, supportStepCol, supportSteps.ToList(), lastDataRow, 4);

        var lastHeaderCol = passportStartCol + passportList.Count * 2 - 1;
        if (lastHeaderCol >= startMonthCol)
        {
            var headerRange = worksheet.Range(1, startMonthCol, 2, lastHeaderCol);
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        }

        foreach (var (columns, text) in headerWidthInstructions)
        {
            ApplyColumnWidthForHeader(worksheet, columns, text);
        }

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
}
