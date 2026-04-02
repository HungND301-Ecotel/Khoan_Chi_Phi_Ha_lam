using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Application.Interfaces.Services;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;

public class ExcelService(IConfiguration configuration) : IExcelService
{

    public byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1", List<string>? hiddenProperties = null, Dictionary<string, List<string>>? dropdownData = null)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);
        worksheet.Style.NumberFormat.Format = "@";
        WriteToWorksheet(worksheet, data, hiddenProperties, dropdownData);
        return SaveWorkbook(workbook);
    }

    public byte[] ExportMultiSheet(Dictionary<string, IEnumerable<object>> sheets, List<string>? hiddenProperties = null)
    {
        using var workbook = new XLWorkbook();
        foreach (var sheetEntry in sheets)
        {
            var worksheet = workbook.Worksheets.Add(sheetEntry.Key);
            // Gọi hàm Generic với kiểu object
            WriteToWorksheet(worksheet, sheetEntry.Value, hiddenProperties, null);
        }
        return SaveWorkbook(workbook);
    }

    private void WriteToWorksheet<T>(IXLWorksheet worksheet, IEnumerable<T> data, List<string>? hiddenProperties, Dictionary<string, List<string>>? dropdownData)
    {
        var dataList = data.ToList();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var workbook = worksheet.Workbook;

        // Tạo sheet ẩn để chứa dữ liệu dropdown nếu danh sách dài
        IXLWorksheet? sourceSheet = null;
        int nextSourceCol = 1;

        for (int i = 0; i < properties.Length; i++)
        {
            var prop = properties[i];
            var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = displayAttr?.Name ?? prop.Name;
            int colIndex = i + 1;

            // Style Header
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F81BD");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // XỬ LÝ DROPDOWN
            if (dropdownData != null && dropdownData.TryGetValue(prop.Name, out var options) && options != null && options.Any())
            {
                if (sourceSheet == null)
                {
                    sourceSheet = workbook.Worksheets.Add("DataSources");
                    sourceSheet.Hide();
                }

                for (int j = 0; j < options.Count; j++)
                {
                    sourceSheet.Cell(j + 1, nextSourceCol).Value = options[j];
                }

                var sourceRange = sourceSheet.Range(1, nextSourceCol, options.Count, nextSourceCol);

                int lastRow = Math.Max(dataList.Count + 1, 100);
                var targetRange = worksheet.Range(2, colIndex, lastRow, colIndex);

                var validation = targetRange.CreateDataValidation();

                validation.List(sourceRange);

                validation.InputTitle = "Lựa chọn";
                validation.InputMessage = "Vui lòng chọn từ danh sách.";
                validation.ErrorStyle = XLErrorStyle.Stop;
                validation.ErrorMessage = "Giá trị không hợp lệ!";

                nextSourceCol++;
            }

            if (hiddenProperties != null && hiddenProperties.Contains(prop.Name))
            {
                worksheet.Column(colIndex).Hide();
            }
        }

        for (int r = 0; r < dataList.Count; r++)
        {
            for (int c = 0; c < properties.Length; c++)
            {
                var value = properties[c].GetValue(dataList[r]);
                var cell = worksheet.Cell(r + 2, c + 1);

                if (value == null)
                {
                    cell.Value = Blank.Value;
                }
                else if (value is DateTime dt)
                {
                    cell.Value = dt;
                }
                else if (value is bool b)
                {
                    cell.Value = b;
                }
                else if (IsNumber(value))
                {
                    cell.Value = Convert.ToDouble(value);
                }
                else
                {
                    string strValue = value.ToString()!;
                    cell.Value = strValue;
                    if (strValue.Contains('\n') || strValue.Contains(';'))
                    {
                        cell.Style.Alignment.SetWrapText(true);
                        cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                    }
                }
            }
        }
    }
    private bool IsNumber(object value) =>
        value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;

    public List<T> ImportFromExcel<T>(Stream fileStream) where T : new()
    {
        var result = new List<T>();
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);
        var rows = worksheet.RangeUsed().RowsUsed();
        var headerRow = rows.First();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var propertyMap = new Dictionary<int, PropertyInfo>();

        for (int col = 1; col <= headerRow.CellCount(); col++)
        {
            var headerValue = headerRow.Cell(col).GetString().Trim();
            var prop = properties.FirstOrDefault(p =>
                p.GetCustomAttribute<DisplayAttribute>()?.Name == headerValue || p.Name == headerValue);
            if (prop != null)
            {
                propertyMap[col] = prop;
            }
        }

        foreach (var row in rows.Skip(1))
        {
            var item = new T();
            bool hasData = false;

            foreach (var entry in propertyMap)
            {
                var cell = row.Cell(entry.Key);
                if (cell.IsEmpty())
                {
                    continue;
                }

                var prop = entry.Value;
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

                try
                {
                    object? val;
                    if (targetType == typeof(Guid))
                    {
                        val = Guid.Parse(cell.GetString());
                    }
                    else if (targetType == typeof(DateOnly))
                    {
                        if (TryParseDateOnly(cell, out var parsedDate))
                        {
                            val = parsedDate;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        val = Convert.ChangeType(cell.Value.ToString(), targetType);
                    }

                    prop.SetValue(item, val);
                    hasData = true;
                }
                catch { /* Log error */ }
            }
            if (hasData)
            {
                result.Add(item);
            }
        }
        return result;
    }
    private static bool TryParseDateOnly(IXLCell cell, out DateOnly date)
    {
        if (cell.TryGetValue<DateTime>(out var cellDateTime))
        {
            date = DateOnly.FromDateTime(cellDateTime);
            return true;
        }

        var raw = cell.GetString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            date = default;
            return false;
        }

        if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateOnly.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date))
        {
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtInvariant))
        {
            date = DateOnly.FromDateTime(dtInvariant);
            return true;
        }

        if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var dtVn))
        {
            date = DateOnly.FromDateTime(dtVn);
            return true;
        }

        date = default;
        return false;
    }
    private byte[] SaveWorkbook(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
