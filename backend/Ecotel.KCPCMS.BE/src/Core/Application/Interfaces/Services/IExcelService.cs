using ClosedXML.Excel;

namespace Application.Interfaces.Services;

public interface IExcelService
{
    // Xuất 1 sheet (Dùng cho Flatten hoặc String-Nested)
    byte[] ExportToExcel<T>(IEnumerable<T> data, string sheetName = "Sheet1", List<string>? hiddenProperties = null, Dictionary<string, List<string>>? dropdownData = null);

    // Xuất nhiều sheet (Cho quan hệ Cha-Con)
    byte[] ExportMultiSheet(Dictionary<string, IEnumerable<object>> sheets, List<string>? hiddenProperties = null);

    // Import tổng quát (Tự động xử lý parse các cột đặc biệt nếu cần)
    List<T> ImportFromExcel<T>(Stream fileStream) where T : new();

    List<T> ImportFromExcelSheet<T>(Stream fileStream, string sheetName) where T : new();

    void AddSheetWithDropdown<T>(XLWorkbook workbook, IEnumerable<T> data, string sheetName, List<string>? hiddenProperties = null, Dictionary<string, List<string>>? dropdownData = null);
}
