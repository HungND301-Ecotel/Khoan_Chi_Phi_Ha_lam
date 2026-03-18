using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LongwallMaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Queries;

public record ExportExcelLongwallMaterialUnitPriceQuery() : IRequest<byte[]>;

public class ExportExcelLongwallMaterialUnitPriceQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<ExportExcelLongwallMaterialUnitPriceQuery, byte[]>
{
    private readonly IWriteRepository<LongwallMaterialUnitPriceEntity> _materialUnitPriceRepository = unitOfWork.GetRepository<LongwallMaterialUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();

    public async Task<byte[]> Handle(ExportExcelLongwallMaterialUnitPriceQuery request, CancellationToken cancellationToken)
    {
        var list = await _materialUnitPriceRepository.GetAllAsync(
            include: s => s
                .Include(s => s.ProductionProcess)
                .Include(s => s.LongwallParameters)
                .Include(s => s.CuttingThickness)
                .Include(s => s.SeamFace)
                .Include(s => s.Technology)
                .Include(s => s.Code),
            disableTracking: true);

        var processes = await _processRepository.GetAllAsync(selector: p => p.Name, disableTracking: true);
        var longwallParametersData = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var longwallParameters = longwallParametersData.Select(l => $"{l.Llc}-{l.Lkc}-{l.Mk}").ToList();
        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(selector: c => c.Value, disableTracking: true);
        var seamFaceEntities = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(selector: s => s.Value, disableTracking: true);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Định mức vật tư lò chợ");

        const int idCol = 1;
        const int startMonthCol = 2;
        const int processCol = 3;
        const int technologyCol = 4;
        const int longwallParametersCol = 5;
        const int cuttingThicknessCol = 6;
        const int seamFaceStartCol = 7;

        var fixedHeaders = new[]
        {
            (idCol, "Id"),
            (startMonthCol, "Thời gian"),
            (processCol, "Công đoạn sản xuất"),
            (technologyCol, "Công nghệ khai thác"),
            (longwallParametersCol, "Thông số lò chợ"),
            (cuttingThicknessCol, "Chiều dày lớp khấu")
        };

        var headerWidthInstructions = new List<(int[] columns, string headerText)>();
        foreach (var (col, title) in fixedHeaders)
        {
            var range = worksheet.Range(1, col, 3, col);
            range.Merge();
            range.Value = title;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            range.Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { col }, title));
        }

        var seamFaceNames = seamFaceEntities.Select(s => s.Value).ToList();
        var seamFaceColumns = seamFaceNames
            .Select((name, index) => new
            {
                name,
                dmCol = seamFaceStartCol + (index * 2),
                ttCol = seamFaceStartCol + (index * 2) + 1
            })
            .ToList();

        foreach (var seamFace in seamFaceColumns)
        {
            var faceRange = worksheet.Range(1, seamFace.dmCol, 2, seamFace.ttCol);
            faceRange.Merge();
            faceRange.Value = seamFace.name;
            faceRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            faceRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            faceRange.Style.Font.Bold = true;
            faceRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            faceRange.Style.Font.FontColor = XLColor.White;
            headerWidthInstructions.Add((new[] { seamFace.dmCol }, "Mã định mức vật liệu"));
            headerWidthInstructions.Add((new[] { seamFace.ttCol }, "TT"));

            worksheet.Cell(3, seamFace.dmCol).Value = "Mã định mức vật liệu";
            worksheet.Cell(3, seamFace.ttCol).Value = "TT";
            worksheet.Cell(3, seamFace.dmCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(3, seamFace.ttCol).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            worksheet.Cell(3, seamFace.dmCol).Style.Font.Bold = true;
            worksheet.Cell(3, seamFace.ttCol).Style.Font.Bold = true;
            worksheet.Cell(3, seamFace.dmCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            worksheet.Cell(3, seamFace.ttCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            worksheet.Cell(3, seamFace.dmCol).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(3, seamFace.ttCol).Style.Font.FontColor = XLColor.White;
        }

        var groupedData = list
            .GroupBy(data => new
            {
                StartMonth = data.StartMonth.ToString("MM/yyyy"),
                ProcessName = data.ProductionProcess?.Name?.Trim() ?? string.Empty,
                TechnologyName = data.Technology?.Value?.Trim() ?? string.Empty,
                LongwallParametersName = data.LongwallParameters != null ? $"{data.LongwallParameters.Llc}-{data.LongwallParameters.Lkc}-{data.LongwallParameters.Mk}" : string.Empty,
                CuttingThicknessName = data.CuttingThickness?.Value?.Trim() ?? string.Empty
            })
            .OrderBy(group => group.Key.StartMonth)
            .ThenBy(group => group.Key.ProcessName)
            .ThenBy(group => group.Key.TechnologyName)
            .ThenBy(group => group.Key.LongwallParametersName)
            .ThenBy(group => group.Key.CuttingThicknessName)
            .ToList();

        var rowIndex = 4;
        foreach (var group in groupedData)
        {
            var representative = group.FirstOrDefault();
            if (representative != null)
            {
                worksheet.Cell(rowIndex, idCol).Value = representative.Id.ToString();
            }

            worksheet.Cell(rowIndex, startMonthCol).Value = group.Key.StartMonth;
            worksheet.Cell(rowIndex, processCol).Value = group.Key.ProcessName;
            worksheet.Cell(rowIndex, technologyCol).Value = group.Key.TechnologyName;
            worksheet.Cell(rowIndex, longwallParametersCol).Value = group.Key.LongwallParametersName;
            worksheet.Cell(rowIndex, cuttingThicknessCol).Value = group.Key.CuttingThicknessName;

            var seamFaceData = group
                .Where(data => !string.IsNullOrWhiteSpace(GetSeamFaceDisplayName(data)))
                .ToDictionary(data => GetSeamFaceDisplayName(data), data => data);

            foreach (var seamFace in seamFaceColumns)
            {
                if (seamFaceData.TryGetValue(seamFace.name, out var entity))
                {
                    worksheet.Cell(rowIndex, seamFace.dmCol).Value = entity.Code?.Value ?? string.Empty;
                    worksheet.Cell(rowIndex, seamFace.ttCol).Value = entity.TotalPrice;
                }
            }

            rowIndex++;
        }

        worksheet.Column(idCol).Hide();

        var lastDataRow = Math.Max(rowIndex - 1, 100);
        AddDropdownValidation(workbook, worksheet, processCol, processes.ToList(), lastDataRow, 1);
        AddDropdownValidation(workbook, worksheet, technologyCol, technologies.ToList(), lastDataRow, 2);
        AddDropdownValidation(workbook, worksheet, longwallParametersCol, longwallParameters, lastDataRow, 3);
        AddDropdownValidation(workbook, worksheet, cuttingThicknessCol, cuttingThicknesses.ToList(), lastDataRow, 4);

        var lastHeaderCol = seamFaceColumns.Any()
            ? seamFaceColumns.Last().ttCol
            : cuttingThicknessCol;

        if (lastHeaderCol >= idCol)
        {
            var headerRange = worksheet.Range(1, idCol, 3, lastHeaderCol);
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

    private static string GetSeamFaceDisplayName(LongwallMaterialUnitPriceEntity data)
    {
        return data.SeamFace?.Value?.Trim() ?? string.Empty;
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
        var targetRange = worksheet.Range(4, targetColumn, lastDataRow, targetColumn);
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
