using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.SlideUnitPrice.Queries;

public record ExportExcelSlideUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelSlideUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelSlideUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();

    public async Task<byte[]> Handle(ExportExcelSlideUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var list = await _slideUnitPriceRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProcessGroup)
                .Include(s => s.Passport)
                .Include(s => s.Hardness)
                .Include(s => s.Code)
                .Include(s => s.SlideUnitPriceAssignmentCodes)
                    .ThenInclude(a => a.Material)
                    .ThenInclude(m => m.Code),
            orderBy: s => s.OrderBy(s => s.StartMonth).ThenBy(s => s.ProcessGroup!.Name).ThenBy(s => s.Hardness!.Value).ThenBy(s => s.Passport!.Name).ThenBy(s => s.Code!.Value),
            disableTracking: true);

        var processGroups = await _processGroupRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(selector: p => $"H/c {p.Name}; {p.Sd}; {p.Sc}", disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(selector: h => h.Value, disableTracking: true);
        var materials = await _materialRepository.GetAllAsync(
            include: m => m.Include(m => m.Code),
            selector: m => m.Code != null ? m.Code.Value : string.Empty,
            disableTracking: true);

        return ExportCustomTemplate(
            list.ToList(),
            processGroups.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            passports.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            hardnesses.Where(x => !string.IsNullOrWhiteSpace(x)).ToList(),
            materials.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList());
    }

    private static byte[] ExportCustomTemplate(
        List<Domain.Entities.Pricing.SlideUnitPrice> slideUnitPrices,
        List<string> processGroups,
        List<string> passports,
        List<string> hardnesses,
        List<string> materialCodes)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức đơn giá trượt");

        const int idCol = 1;
        const int startMonthCol = 2;
        const int endMonthCol = 3;
        const int processGroupCol = 4;
        const int hardnessCol = 5;
        const int materialCodeCol = 6;
        const int passportStartCol = 7;

        var fixedHeaders = new[]
        {
            (idCol, "Id"),
            (startMonthCol, "Thời gian bắt đầu"),
            (endMonthCol, "Thời gian kết thúc"),
            (processGroupCol, "Nhóm công đoạn"),
            (hardnessCol, "Độ kiên cố than đá"),
            (materialCodeCol, "Mã vật tư")
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

        var passportColumns = passports
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

            worksheet.Cell(2, passport.dmCol).Value = "Mã định mức máng trượt";
            worksheet.Cell(2, passport.ttCol).Value = "Tổng tiền";
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

        var groupedData = slideUnitPrices
            .GroupBy(s => new
            {
                StartMonth = s.StartMonth.ToString("MM/yyyy"),
                EndMonth = s.EndMonth.ToString("MM/yyyy"),
                ProcessGroupName = s.ProcessGroup?.Name?.Trim() ?? string.Empty,
                HardnessName = s.Hardness?.Value?.Trim() ?? string.Empty,
                MaterialSignature = string.Join("|", GetMaterialCodes(s))
            })
            .OrderBy(g => g.Key.StartMonth)
            .ThenBy(g => g.Key.ProcessGroupName)
            .ThenBy(g => g.Key.HardnessName)
            .ThenBy(g => g.Key.MaterialSignature)
            .ToList();

        var rowIndex = 3;
        foreach (var group in groupedData)
        {
            var representative = group.First();
            var materialList = GetMaterialCodes(representative);

            if (!materialList.Any())
            {
                materialList.Add(string.Empty);
            }

            var groupStartRow = rowIndex;
            var groupEndRow = rowIndex + materialList.Count - 1;

            var passportMap = group
                .Where(s => !string.IsNullOrWhiteSpace(GetPassportDisplayName(s)))
                .GroupBy(GetPassportDisplayName)
                .ToDictionary(g => g.Key, g => g.First());

            for (var i = 0; i < materialList.Count; i++)
            {
                var currentRow = rowIndex + i;
                var materialCode = materialList[i];
                worksheet.Cell(currentRow, materialCodeCol).Value = materialCode;

                if (i == 0)
                {
                    worksheet.Cell(currentRow, idCol).Value = representative.Id.ToString();
                    worksheet.Cell(currentRow, startMonthCol).Value = group.Key.StartMonth;
                    worksheet.Cell(currentRow, endMonthCol).Value = group.Key.EndMonth;
                    worksheet.Cell(currentRow, processGroupCol).Value = group.Key.ProcessGroupName;
                    worksheet.Cell(currentRow, hardnessCol).Value = group.Key.HardnessName;

                    foreach (var passportColumn in passportColumns)
                    {
                        if (!passportMap.TryGetValue(passportColumn.name, out var entity))
                        {
                            continue;
                        }

                        worksheet.Cell(currentRow, passportColumn.dmCol).Value = entity.Code?.Value ?? string.Empty;
                    }
                }

                foreach (var passportColumn in passportColumns)
                {
                    if (!passportMap.TryGetValue(passportColumn.name, out var entity))
                    {
                        continue;
                    }

                    var amount = GetMaterialAmount(entity, materialCode);
                    if (amount.HasValue)
                    {
                        worksheet.Cell(currentRow, passportColumn.ttCol).Value = amount.Value;
                        worksheet.Cell(currentRow, passportColumn.ttCol).Style.NumberFormat.Format = "0.00";
                    }
                }
            }

            if (groupEndRow > groupStartRow)
            {
                worksheet.Range(groupStartRow, idCol, groupEndRow, idCol).Merge();
                worksheet.Range(groupStartRow, startMonthCol, groupEndRow, startMonthCol).Merge();
                worksheet.Range(groupStartRow, endMonthCol, groupEndRow, endMonthCol).Merge();
                worksheet.Range(groupStartRow, processGroupCol, groupEndRow, processGroupCol).Merge();
                worksheet.Range(groupStartRow, hardnessCol, groupEndRow, hardnessCol).Merge();
            }

            rowIndex += materialList.Count;
        }

        worksheet.Column(idCol).Hide();

        var lastDataRow = Math.Max(rowIndex - 1, 100);
        AddDropdownValidation(workbook, worksheet, processGroupCol, processGroups, lastDataRow, 1);
        AddDropdownValidation(workbook, worksheet, hardnessCol, hardnesses, lastDataRow, 2);
        AddDropdownValidation(workbook, worksheet, materialCodeCol, materialCodes, lastDataRow, 3);

        var lastHeaderCol = passportColumns.Any()
            ? passportColumns.Last().ttCol
            : materialCodeCol;

        var headerRange = worksheet.Range(1, idCol, 2, lastHeaderCol);
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        foreach (var (columns, text) in headerWidthInstructions)
        {
            ApplyColumnWidthForHeader(worksheet, columns, text);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string GetPassportDisplayName(Domain.Entities.Pricing.SlideUnitPrice data)
    {
        if (data.Passport == null)
        {
            return string.Empty;
        }

        return $"H/c {data.Passport.Name}; {data.Passport.Sd}; {data.Passport.Sc}";
    }

    private static List<string> GetMaterialCodes(Domain.Entities.Pricing.SlideUnitPrice data)
    {
        return data.SlideUnitPriceAssignmentCodes
            .Select(a => a.Material?.Code?.Value?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .OrderBy(code => code)
            .ToList();
    }

    private static double? GetMaterialAmount(Domain.Entities.Pricing.SlideUnitPrice data, string materialCode)
    {
        if (string.IsNullOrWhiteSpace(materialCode))
        {
            return null;
        }

        var assignment = data.SlideUnitPriceAssignmentCodes
            .FirstOrDefault(a => string.Equals(a.Material?.Code?.Value?.Trim(), materialCode.Trim(), StringComparison.OrdinalIgnoreCase));

        return assignment?.Amount;
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
}
