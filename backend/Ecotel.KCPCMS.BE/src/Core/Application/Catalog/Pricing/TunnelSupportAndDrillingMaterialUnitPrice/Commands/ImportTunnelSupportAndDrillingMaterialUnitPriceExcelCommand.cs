using System.Globalization;
using System.Text.RegularExpressions;
using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.TunnelSupportAndDrillingMaterialPricing.Commands;

public record ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommand, bool>
{
    private const string OtherMaterialDisplay = "VTK - Vật tư khác";
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "TunnelSupportAndDrillingMaterialUnitPrice";

    private readonly IWriteRepository<TunnelSupportAndDrillingMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelSupportAndDrillingMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<MaterialUnitPriceAssignmentCode>();

    public async Task<bool> Handle(ImportTunnelSupportAndDrillingMaterialUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        var importErrors = new List<string>();

        var processes = await _processRepository.GetAllAsync(disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(disableTracking: true);
        var assignments = await _assignmentCodeRepository.GetAllAsync(
            include: a => a.Include(x => x.Code),
            disableTracking: true);

        var assignmentLookup = BuildAssignmentLookup(assignments);

        using var stream = request.File.OpenReadStream();
        var importRows = ParseFromCustomTemplate(stream, assignmentLookup, importErrors);
        ThrowIfImportErrors(importErrors);

        if (!importRows.Any())
        {
            throw new BadRequestException("Không có dữ liệu hợp lệ để import.");
        }

        var duplicateCodes = importRows
            .GroupBy(d => d.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateCodes.Any())
        {
            importErrors.Add($"File Excel có mã định mức vật liệu bị trùng: {string.Join("; ", duplicateCodes)}");
        }

        ThrowIfImportErrors(importErrors);

        var processIdMap = processes
            .ToDictionary(p => NormalizeLookupValue(p.Name), p => p.Id, StringComparer.OrdinalIgnoreCase);
        var passportIdMap = passports
            .ToDictionary(p => NormalizeLookupValue($"H/c {p.Name}; {p.Sd}; {p.Sc}"), p => p.Id, StringComparer.OrdinalIgnoreCase);
        var hardnessIdMap = hardnesses
            .ToDictionary(h => NormalizeLookupValue(h.Value), h => h.Id, StringComparer.OrdinalIgnoreCase);
        var technologyIdMap = technologies
            .Where(t => !string.IsNullOrWhiteSpace(t.Value))
            .ToDictionary(t => NormalizeLookupValue(t.Value), t => t.Id, StringComparer.OrdinalIgnoreCase);

        var dbEntities = await _materialUnitPriceRepository.GetAllAsync(
            include: a => a
                .Include(a => a.Code!)
                .Include(a => a.MaterialUnitPriceAssignmentCodes),
            disableTracking: false);

        var dbCodeLookup = new Dictionary<string, TunnelSupportAndDrillingMaterialUnitPrice>(StringComparer.OrdinalIgnoreCase);
        foreach (var entity in dbEntities)
        {
            var code = entity.Code?.Value?.Trim();
            if (string.IsNullOrWhiteSpace(code) || dbCodeLookup.ContainsKey(code))
            {
                continue;
            }

            dbCodeLookup[code] = entity;
        }

        var matchedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var updateList = new List<TunnelSupportAndDrillingMaterialUnitPrice>();
        var addList = new List<TunnelSupportAndDrillingMaterialUnitPrice>();
        var assignmentCostsToDelete = new List<MaterialUnitPriceAssignmentCode>();

        foreach (var row in importRows)
        {
            var code = row.Code.Trim();
            try
            {
                if (!processIdMap.TryGetValue(NormalizeLookupValue(row.ProcessName), out var processId))
                {
                    importErrors.Add($"Công đoạn sản xuất '{row.ProcessName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                if (!passportIdMap.TryGetValue(NormalizeLookupValue(row.PassportName), out var passportId))
                {
                    importErrors.Add($"Hộ chiếu '{row.PassportName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                if (!hardnessIdMap.TryGetValue(NormalizeLookupValue(row.HardnessName), out var hardnessId))
                {
                    importErrors.Add($"Độ kiên cố '{row.HardnessName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                Guid? technologyId = null;
                if (!string.IsNullOrWhiteSpace(row.TechnologyName))
                {
                    if (!technologyIdMap.TryGetValue(NormalizeLookupValue(row.TechnologyName), out var mappedTechnologyId))
                    {
                        importErrors.Add($"Công nghệ '{row.TechnologyName}' không tồn tại cho mã '{code}'.");
                        continue;
                    }

                    technologyId = mappedTechnologyId;
                }

                var startMonth = ParseMonthYear(row.StartMonth);
                var endMonth = ParseMonthYear(row.EndMonth);

                var costDtos = row.AssignmentCosts
                    .Select(c => new MaterialUnitPriceAssignmentCodeDto
                    {
                        AssignmentCodeId = c.AssignmentCodeId,
                        TotalPrice = c.TotalPrice
                    })
                    .ToList();
                var costs = costDtos.Adapt<List<MaterialUnitPriceAssignmentCode>>();

                if (dbCodeLookup.TryGetValue(code, out var existingEntity))
                {
                    if (existingEntity.MaterialUnitPriceAssignmentCodes.Any())
                    {
                        assignmentCostsToDelete.AddRange(existingEntity.MaterialUnitPriceAssignmentCodes);
                    }

                    existingEntity.Update(
                        code,
                        processId,
                        passportId,
                        hardnessId,
                        technologyId,
                        startMonth,
                        endMonth,
                        row.OtherMaterialValue,
                        costs);
                    updateList.Add(existingEntity);
                    matchedCodes.Add(code);
                }
                else
                {
                    var newEntity = TunnelSupportAndDrillingMaterialUnitPrice.Create(
                        code,
                        processId,
                        passportId,
                        hardnessId,
                        technologyId,
                        startMonth,
                        endMonth,
                        row.OtherMaterialValue,
                        costs);
                    addList.Add(newEntity);
                    matchedCodes.Add(code);
                }
            }
            catch (Exception ex) when (ex is BadRequestException or ConflictException)
            {
                importErrors.Add($"Mã '{code}': {ex.Message}");
            }
        }

        var deleteList = dbEntities
            .Where(entity =>
            {
                var code = entity.Code?.Value?.Trim();
                return !string.IsNullOrWhiteSpace(code) && !matchedCodes.Contains(code);
            })
            .ToList();

        ThrowIfImportErrors(importErrors);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (assignmentCostsToDelete.Any())
            {
                _materialUnitPriceAssignmentCodeRepository.Delete(assignmentCostsToDelete);
            }

            if (deleteList.Any())
            {
                _materialUnitPriceRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _materialUnitPriceRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _materialUnitPriceRepository.Update(updateList);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitAsync(cancellationToken);
            cacheService.InvalidateGroup(CacheSignalKey);
            cacheService.InvalidateGroup(ModuleCacheSignalKey);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private static List<ParsedTunnelSupportAndDrillingRow> ParseFromCustomTemplate(
        Stream fileStream,
        IReadOnlyDictionary<string, AssignmentLookupItem> assignmentLookup,
        ICollection<string> importErrors)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        const int startMonthCol = 1;
        const int endMonthCol = 2;
        const int processCol = 3;
        const int hardnessCol = 4;
        const int technologyCol = 5;
        const int assignmentCol = 6;
        const int passportStartCol = 7;

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 3 || lastCol < passportStartCol)
        {
            return [];
        }

        var passportPositions = new List<(int valueCol, string name)>();
        for (var col = passportStartCol; col <= lastCol; col++)
        {
            var passportName = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(passportName))
            {
                continue;
            }

            passportPositions.Add((col, passportName));
        }

        var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("MM/yyyy");
        var aggregates = new Dictionary<TunnelSupportAndDrillingRowKey, ParsedTunnelSupportAndDrillingRow>();
        TunnelSupportAndDrillingRowContext? currentContext = null;

        for (var row = 3; row <= lastRow; row++)
        {
            var assignmentText = worksheet.Cell(row, assignmentCol).GetString().Trim();
            var startMonth = worksheet.Cell(row, startMonthCol).GetString().Trim();
            var endMonth = worksheet.Cell(row, endMonthCol).GetString().Trim();
            var processName = worksheet.Cell(row, processCol).GetString().Trim();
            var hardnessName = worksheet.Cell(row, hardnessCol).GetString().Trim();
            var technologyName = worksheet.Cell(row, technologyCol).GetString().Trim();

            var passportCodeByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(assignmentText))
            {
                foreach (var passport in passportPositions)
                {
                    var codeText = worksheet.Cell(row, passport.valueCol).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(codeText))
                    {
                        passportCodeByName[passport.name] = codeText;
                    }
                }

                var hasBaseInfo =
                    !string.IsNullOrWhiteSpace(startMonth) ||
                    !string.IsNullOrWhiteSpace(endMonth) ||
                    !string.IsNullOrWhiteSpace(processName) ||
                    !string.IsNullOrWhiteSpace(hardnessName) ||
                    !string.IsNullOrWhiteSpace(technologyName) ||
                    passportCodeByName.Any();

                if (!hasBaseInfo)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(startMonth))
                {
                    startMonth = currentMonth;
                }

                if (string.IsNullOrWhiteSpace(endMonth))
                {
                    endMonth = startMonth;
                }

                currentContext = new TunnelSupportAndDrillingRowContext(
                    StartMonth: startMonth,
                    EndMonth: endMonth,
                    ProcessName: processName,
                    HardnessName: hardnessName,
                    TechnologyName: technologyName,
                    PassportCodeByName: passportCodeByName);

                foreach (var passport in passportCodeByName)
                {
                    var key = new TunnelSupportAndDrillingRowKey(
                        Code: passport.Value,
                        PassportName: passport.Key,
                        StartMonth: startMonth,
                        EndMonth: endMonth,
                        ProcessName: processName,
                        HardnessName: hardnessName,
                        TechnologyName: technologyName);

                    if (aggregates.ContainsKey(key))
                    {
                        importErrors.Add(
                            $"Dữ liệu bị trùng mã định mức vật liệu '{passport.Value}' trong cùng bộ thông số (dòng {row}).");
                        continue;
                    }

                    aggregates[key] = new ParsedTunnelSupportAndDrillingRow(
                        code: passport.Value,
                        processName: processName,
                        passportName: passport.Key,
                        hardnessName: hardnessName,
                        technologyName: technologyName,
                        startMonth: startMonth,
                        endMonth: endMonth);
                }

                continue;
            }

            if (currentContext is null)
            {
                continue;
            }

            var hasAnyTt = passportPositions.Any(p => !worksheet.Cell(row, p.valueCol).IsEmpty());
            if (!hasAnyTt)
            {
                continue;
            }

            var isOtherMaterial = IsOtherMaterialAssignment(assignmentText);
            AssignmentLookupItem? assignment = null;

            if (!isOtherMaterial)
            {
                var assignmentKey = NormalizeLookupValue(assignmentText);
                if (!assignmentLookup.TryGetValue(assignmentKey, out assignment))
                {
                    importErrors.Add($"Nhóm vật tư, tài sản '{assignmentText}' không tồn tại ở dòng {row}.");
                    continue;
                }
            }

            foreach (var passport in passportPositions)
            {
                var ttCell = worksheet.Cell(row, passport.valueCol);
                if (ttCell.IsEmpty())
                {
                    continue;
                }

                if (!TryParseDouble(ttCell, out var totalPrice))
                {
                    importErrors.Add($"Giá trị TT không hợp lệ tại dòng {row}, cột {passport.valueCol}.");
                    continue;
                }

                if (!currentContext.PassportCodeByName.TryGetValue(passport.name, out var materialCode)
                    || string.IsNullOrWhiteSpace(materialCode))
                {
                    importErrors.Add(
                        $"Thiếu mã định mức vật liệu cho hộ chiếu '{passport.name}' trước khi nhập TT (dòng {row}, cột {passport.valueCol}).");
                    continue;
                }

                var key = new TunnelSupportAndDrillingRowKey(
                    Code: materialCode,
                    PassportName: passport.name,
                    StartMonth: currentContext.StartMonth,
                    EndMonth: currentContext.EndMonth,
                    ProcessName: currentContext.ProcessName,
                    HardnessName: currentContext.HardnessName,
                    TechnologyName: currentContext.TechnologyName);

                if (!aggregates.TryGetValue(key, out var aggregate))
                {
                    importErrors.Add(
                        $"Không tìm thấy dòng thông số cho mã định mức vật liệu '{materialCode}' trước dòng {row}.");
                    continue;
                }

                if (isOtherMaterial)
                {
                    aggregate.AddOtherMaterial(totalPrice);
                    continue;
                }

                if (assignment == null)
                {
                    importErrors.Add($"Nhóm vật tư, tài sản '{assignmentText}' không hợp lệ ở dòng {row}.");
                    continue;
                }

                try
                {
                    aggregate.AddAssignmentCost(assignment.Id, totalPrice, assignment.Display);
                }
                catch (BadRequestException ex)
                {
                    importErrors.Add(ex.Message);
                }
            }
        }

        return aggregates.Values.ToList();
    }

    private static IReadOnlyDictionary<string, AssignmentLookupItem> BuildAssignmentLookup(IEnumerable<AssignmentCode> assignments)
    {
        var lookup = new Dictionary<string, AssignmentLookupItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var assignment in assignments)
        {
            var code = assignment.Code?.Value?.Trim() ?? string.Empty;
            var name = assignment.Name?.Trim() ?? string.Empty;
            var display = BuildAssignmentDisplay(code, name);
            var item = new AssignmentLookupItem(assignment.Id, display);

            if (!string.IsNullOrWhiteSpace(code))
            {
                lookup.TryAdd(NormalizeLookupValue(code), item);
            }

            if (!string.IsNullOrWhiteSpace(display))
            {
                lookup.TryAdd(NormalizeLookupValue(display), item);
            }
        }

        return lookup;
    }

    private static string BuildAssignmentDisplay(string code, string name)
    {
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
        {
            return $"{code} - {name}";
        }

        return !string.IsNullOrWhiteSpace(code) ? code : name;
    }

    private static bool IsOtherMaterialAssignment(string assignmentText)
    {
        var normalized = NormalizeLookupValue(assignmentText);
        return normalized == NormalizeLookupValue(OtherMaterialDisplay)
            || normalized == "VTK"
            || normalized.StartsWith("VTK ", StringComparison.OrdinalIgnoreCase);
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            var now = DateTime.Now;
            return new DateOnly(now.Year, now.Month, 1);
        }

        if (DateOnly.TryParseExact(monthYear, "MM/yyyy", null, DateTimeStyles.None, out var result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(monthYear, "M/yyyy", null, DateTimeStyles.None, out result))
        {
            return result;
        }

        if (DateTime.TryParse(monthYear, out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        throw new BadRequestException($"Không thể parse tháng năm: {monthYear}. Định dạng cần là MM/yyyy hoặc M/yyyy");
    }

    private static bool TryParseDouble(IXLCell cell, out double value)
    {
        if (cell.DataType == XLDataType.Number)
        {
            value = cell.GetDouble();
            return true;
        }

        var text = cell.GetString().Trim();
        return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
            || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
    }

    private static string NormalizeLookupValue(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input.Trim(), @"\s+", " ").ToUpperInvariant();
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        var errors = importErrors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ExcelImportException(errors);
    }

    private sealed record AssignmentLookupItem(Guid Id, string Display);

    private sealed record TunnelSupportAndDrillingRowContext(
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string HardnessName,
        string TechnologyName,
        IReadOnlyDictionary<string, string> PassportCodeByName);

    private sealed record TunnelSupportAndDrillingRowKey(
        string Code,
        string PassportName,
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string HardnessName,
        string TechnologyName);

    private sealed class ParsedTunnelSupportAndDrillingRow(
        string code,
        string processName,
        string passportName,
        string hardnessName,
        string technologyName,
        string startMonth,
        string endMonth)
    {
        private readonly Dictionary<Guid, AssignmentCostImportItem> _assignmentCostMap = new();

        public string Code { get; } = code;
        public string ProcessName { get; } = processName;
        public string PassportName { get; } = passportName;
        public string HardnessName { get; } = hardnessName;
        public string TechnologyName { get; } = technologyName;
        public string StartMonth { get; } = startMonth;
        public string EndMonth { get; } = endMonth;
        public double OtherMaterialValue { get; private set; }
        public IReadOnlyCollection<AssignmentCostImportItem> AssignmentCosts => _assignmentCostMap.Values;

        public void AddOtherMaterial(double amount)
        {
            OtherMaterialValue += amount;
        }

        public void AddAssignmentCost(Guid assignmentCodeId, double amount, string assignmentDisplay)
        {
            if (_assignmentCostMap.ContainsKey(assignmentCodeId))
            {
                throw new BadRequestException(
                    $"Nhóm vật tư, tài sản '{assignmentDisplay}' bị trùng cho mã định mức vật liệu '{Code}'.");
            }

            _assignmentCostMap[assignmentCodeId] = new AssignmentCostImportItem(assignmentCodeId, amount);
        }
    }

    private sealed record AssignmentCostImportItem(Guid AssignmentCodeId, double TotalPrice);
}
