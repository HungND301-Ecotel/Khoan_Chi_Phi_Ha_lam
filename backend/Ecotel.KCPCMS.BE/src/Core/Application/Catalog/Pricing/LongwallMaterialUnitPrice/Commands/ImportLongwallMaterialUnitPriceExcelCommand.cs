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
using Shared.Constants;
using LongwallMaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;

public record ImportLongwallMaterialUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportLongwallMaterialUnitPriceExcelCommandHandler(IUnitOfWork unitOfWork, ICacheService cacheService) : IRequestHandler<ImportLongwallMaterialUnitPriceExcelCommand, bool>
{
    private const string OtherMaterialDisplay = "VTK - Vật tư khác";
    private const string LegacyNoneOptionDisplay = "Không";
    private const string ProductUnitPriceCacheSignalKey = "ProductUnitPrice";
    private const string LongwallMaterialUnitPriceCacheSignalKey = "LongwallMaterialUnitPrice";

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
    private readonly IWriteRepository<MaterialUnitPriceAssignmentCode> _materialUnitPriceAssignmentCodeRepository = unitOfWork.GetRepository<MaterialUnitPriceAssignmentCode>();

    public async Task<bool> Handle(ImportLongwallMaterialUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        var importErrors = new List<string>();

        var processes = await _processRepository.GetAllAsync(disableTracking: true);
        var longwallParametersList = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(disableTracking: true);
        var seamFaces = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(disableTracking: true);
        var powers = await _powerRepository.GetAllAsync(disableTracking: true);
        var assignments = await _assignmentCodeRepository.GetAllAsync(
            include: a => a.Include(x => x.Code),
            disableTracking: true);
        var materials = await _materialRepository.GetAllAsync(
            include: m => m
                .Include(x => x.Code)
                .Include(x => x.Costs)
                .Include(x => x.AssignmentCodeMaterials),
            disableTracking: true);

        var assignmentLookup = BuildAssignmentLookup(assignments);
        var materialLookup = BuildMaterialLookup(materials);
        var materialById = materials.ToDictionary(m => m.Id);

        using var stream = request.File.OpenReadStream();
        var importRows = ParseFromCustomTemplate(stream, assignmentLookup, materialLookup, importErrors);
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
        var longwallParametersIdMap = longwallParametersList
            .ToDictionary(l => NormalizeLookupValue($"{l.Llc}-{l.Lkc}-{l.Mk}"), l => l.Id, StringComparer.OrdinalIgnoreCase);
        var cuttingThicknessIdMap = cuttingThicknesses
            .ToDictionary(c => NormalizeLookupValue(c.Value), c => c.Id, StringComparer.OrdinalIgnoreCase);
        var seamFaceIdMap = seamFaces
            .ToDictionary(s => NormalizeLookupValue(s.Value), s => s.Id, StringComparer.OrdinalIgnoreCase);
        var technologyIdMap = technologies
            .Where(t => !string.IsNullOrWhiteSpace(t.Value))
            .ToDictionary(t => NormalizeLookupValue(t.Value), t => t.Id, StringComparer.OrdinalIgnoreCase);
        var hardnessIdMap = hardnesses
            .Where(h => !string.IsNullOrWhiteSpace(h.Value))
            .ToDictionary(h => NormalizeLookupValue(h.Value), h => h.Id, StringComparer.OrdinalIgnoreCase);
        var powerIdMap = powers
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .ToDictionary(p => NormalizeLookupValue(p.Value), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var dbEntities = await _materialUnitPriceRepository.GetAllAsync(
            include: e => e
                .Include(e => e.Code)
                .Include(e => e.MaterialUnitPriceAssignmentCodes),
            disableTracking: false);

        var dbCodeLookup = new Dictionary<string, LongwallMaterialUnitPriceEntity>(StringComparer.OrdinalIgnoreCase);
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
        var updateList = new List<LongwallMaterialUnitPriceEntity>();
        var addList = new List<LongwallMaterialUnitPriceEntity>();
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

                if (!longwallParametersIdMap.TryGetValue(NormalizeLookupValue(row.LongwallParametersName), out var longwallParametersId))
                {
                    importErrors.Add($"Thông số lò chợ '{row.LongwallParametersName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                if (!cuttingThicknessIdMap.TryGetValue(NormalizeLookupValue(row.CuttingThicknessName), out var cuttingThicknessId))
                {
                    importErrors.Add($"Chiều dày lớp khấu '{row.CuttingThicknessName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                if (!seamFaceIdMap.TryGetValue(NormalizeLookupValue(row.SeamFaceName), out var seamFaceId))
                {
                    importErrors.Add($"Mặt vỉa '{row.SeamFaceName}' không tồn tại cho mã '{code}'.");
                    continue;
                }

                Guid? technologyId = null;
                if (!string.IsNullOrWhiteSpace(row.TechnologyName))
                {
                    if (!technologyIdMap.TryGetValue(NormalizeLookupValue(row.TechnologyName), out var mappedTechnologyId))
                    {
                        importErrors.Add($"Công nghệ khai thác '{row.TechnologyName}' không tồn tại cho mã '{code}'.");
                        continue;
                    }

                    technologyId = mappedTechnologyId;
                }

                var startMonth = ParseMonthYear(row.StartMonth);
                var endMonth = ParseMonthYear(row.EndMonth);
                var (powerId, hardnessId) = ResolvePowerAndHardness(row.PowerName, row.HardnessName, powerIdMap, hardnessIdMap, code);

                var costDtos = row.AssignmentCosts
                    .Select(c => new MaterialUnitPriceAssignmentCodeDto
                    {
                        AssignmentCodeId = c.AssignmentCodeId,
                        MaterialId = c.MaterialId,
                        Norm = c.Norm,
                        TotalPrice = (materialById.TryGetValue(c.MaterialId, out var material)
                                ? material.GetMaterialCost(startMonth)
                                : 0) * c.Norm
                    })
                    .ToList();
                var costs = costDtos
                    .Select(cost => MaterialUnitPriceAssignmentCode.Create(
                        cost.AssignmentCodeId,
                        cost.TotalPrice,
                        cost.MaterialId,
                        cost.Norm))
                    .ToList();

                if (dbCodeLookup.TryGetValue(code, out var existing))
                {
                    if (existing.MaterialUnitPriceAssignmentCodes.Any())
                    {
                        assignmentCostsToDelete.AddRange(existing.MaterialUnitPriceAssignmentCodes);
                    }

                    existing.Update(
                        code,
                        processId,
                        longwallParametersId,
                        cuttingThicknessId,
                        seamFaceId,
                        powerId,
                        hardnessId,
                        powerId.HasValue,
                        technologyId,
                        startMonth,
                        endMonth,
                        row.OtherMaterialValue,
                        costs);
                    updateList.Add(existing);
                    matchedCodes.Add(code);
                }
                else
                {
                    var newEntity = LongwallMaterialUnitPriceEntity.Create(
                        code,
                        processId,
                        longwallParametersId,
                        cuttingThicknessId,
                        seamFaceId,
                        powerId,
                        hardnessId,
                        powerId.HasValue,
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

        try
        {
            ValidateMonthRangeOverlap(dbEntities, addList, deleteList);
        }
        catch (ConflictException ex)
        {
            importErrors.Add(ex.Message);
        }

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

            cacheService.InvalidateGroup(ProductUnitPriceCacheSignalKey);
            cacheService.InvalidateGroup(LongwallMaterialUnitPriceCacheSignalKey);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static List<ParsedLongwallMaterialUnitPriceRow> ParseFromCustomTemplate(
        Stream fileStream,
        IReadOnlyDictionary<string, AssignmentLookupItem> assignmentLookup,
        IReadOnlyDictionary<string, MaterialLookupItem> materialLookup,
        ICollection<string> importErrors)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

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

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 3 || lastCol < seamFaceStartCol)
        {
            return [];
        }

        var seamFacePositions = new List<(int valueCol, string name)>();
        for (var col = seamFaceStartCol; col <= lastCol; col++)
        {
            var seamFaceName = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(seamFaceName))
            {
                continue;
            }

            seamFacePositions.Add((col, seamFaceName));
        }

        var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("MM/yyyy");
        var aggregates = new Dictionary<LongwallRowKey, ParsedLongwallMaterialUnitPriceRow>();
        LongwallRowContext? currentContext = null;

        for (var row = 3; row <= lastRow; row++)
        {
            var assignmentText = worksheet.Cell(row, assignmentCol).GetString().Trim();
            var materialText = worksheet.Cell(row, materialCol).GetString().Trim();
            var startMonth = worksheet.Cell(row, startMonthCol).GetString().Trim();
            var endMonth = worksheet.Cell(row, endMonthCol).GetString().Trim();
            var processName = worksheet.Cell(row, processCol).GetString().Trim();
            var technologyName = worksheet.Cell(row, technologyCol).GetString().Trim();
            var hardnessName = worksheet.Cell(row, hardnessCol).GetString().Trim();
            var powerName = worksheet.Cell(row, powerCol).GetString().Trim();
            var longwallParametersName = worksheet.Cell(row, longwallParametersCol).GetString().Trim();
            var cuttingThicknessName = worksheet.Cell(row, cuttingThicknessCol).GetString().Trim();

            var seamFaceCodeByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(assignmentText) && string.IsNullOrWhiteSpace(materialText))
            {
                foreach (var seamFace in seamFacePositions)
                {
                    var codeText = worksheet.Cell(row, seamFace.valueCol).GetString().Trim();
                    if (!string.IsNullOrWhiteSpace(codeText))
                    {
                        seamFaceCodeByName[seamFace.name] = codeText;
                    }
                }

                var hasBaseInfo =
                    !string.IsNullOrWhiteSpace(startMonth) ||
                    !string.IsNullOrWhiteSpace(endMonth) ||
                    !string.IsNullOrWhiteSpace(processName) ||
                    !string.IsNullOrWhiteSpace(technologyName) ||
                    !string.IsNullOrWhiteSpace(hardnessName) ||
                    !string.IsNullOrWhiteSpace(powerName) ||
                    !string.IsNullOrWhiteSpace(longwallParametersName) ||
                    !string.IsNullOrWhiteSpace(cuttingThicknessName) ||
                    seamFaceCodeByName.Any();

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

                currentContext = new LongwallRowContext(
                    StartMonth: startMonth,
                    EndMonth: endMonth,
                    ProcessName: processName,
                    TechnologyName: technologyName,
                    HardnessName: hardnessName,
                    PowerName: powerName,
                    LongwallParametersName: longwallParametersName,
                    CuttingThicknessName: cuttingThicknessName,
                    SeamFaceCodeByName: seamFaceCodeByName);

                foreach (var seamFace in seamFaceCodeByName)
                {
                    var key = new LongwallRowKey(
                        Code: seamFace.Value,
                        SeamFaceName: seamFace.Key,
                        StartMonth: startMonth,
                        EndMonth: endMonth,
                        ProcessName: processName,
                        TechnologyName: technologyName,
                        HardnessName: hardnessName,
                        PowerName: powerName,
                        LongwallParametersName: longwallParametersName,
                        CuttingThicknessName: cuttingThicknessName);

                    if (aggregates.ContainsKey(key))
                    {
                        importErrors.Add(
                            $"Dữ liệu bị trùng mã định mức vật liệu '{seamFace.Value}' trong cùng bộ thông số (dòng {row}).");
                        continue;
                    }

                    aggregates[key] = new ParsedLongwallMaterialUnitPriceRow(
                        code: seamFace.Value,
                        processName: processName,
                        technologyName: technologyName,
                        hardnessName: hardnessName,
                        powerName: powerName,
                        longwallParametersName: longwallParametersName,
                        cuttingThicknessName: cuttingThicknessName,
                        seamFaceName: seamFace.Key,
                        startMonth: startMonth,
                        endMonth: endMonth);
                }

                continue;
            }

            if (currentContext is null)
            {
                continue;
            }

            var hasAnyTt = seamFacePositions.Any(p => !worksheet.Cell(row, p.valueCol).IsEmpty());
            if (!hasAnyTt)
            {
                continue;
            }

            var isOtherMaterial = IsOtherMaterialAssignment(assignmentText);
            AssignmentLookupItem? assignment = null;
            MaterialLookupItem? material = null;

            if (!isOtherMaterial)
            {
                var assignmentKey = NormalizeLookupValue(assignmentText);
                if (!assignmentLookup.TryGetValue(assignmentKey, out assignment))
                {
                    importErrors.Add($"Nhóm vật tư, tài sản '{assignmentText}' không tồn tại ở dòng {row}.");
                    continue;
                }

                var materialKey = NormalizeLookupValue(materialText);
                if (!materialLookup.TryGetValue(materialKey, out material))
                {
                    importErrors.Add($"Vật tư tài sản '{materialText}' không tồn tại ở dòng {row}.");
                    continue;
                }

                if (!material.AssignmentCodeIds.Contains(assignment.Id))
                {
                    importErrors.Add(
                        $"Vật tư tài sản '{materialText}' không thuộc nhóm vật tư, tài sản '{assignmentText}' ở dòng {row}.");
                    continue;
                }
            }
            else if (!string.IsNullOrWhiteSpace(materialText) &&
                     NormalizeLookupValue(materialText) != NormalizeLookupValue(OtherMaterialDisplay) &&
                     NormalizeLookupValue(materialText) != "VTK")
            {
                importErrors.Add($"Vật tư tài sản '{materialText}' không hợp lệ cho dòng VTK ở dòng {row}.");
                continue;
            }

            foreach (var seamFace in seamFacePositions)
            {
                var ttCell = worksheet.Cell(row, seamFace.valueCol);
                if (ttCell.IsEmpty())
                {
                    continue;
                }

                if (!TryParseDouble(ttCell, out var norm))
                {
                    importErrors.Add($"Giá trị định mức không hợp lệ tại dòng {row}, cột {seamFace.valueCol}.");
                    continue;
                }

                if (!currentContext.SeamFaceCodeByName.TryGetValue(seamFace.name, out var materialCode)
                    || string.IsNullOrWhiteSpace(materialCode))
                {
                    importErrors.Add(
                        $"Thiếu mã định mức vật liệu cho mặt vỉa '{seamFace.name}' trước khi nhập TT (dòng {row}, cột {seamFace.valueCol}).");
                    continue;
                }

                var key = new LongwallRowKey(
                    Code: materialCode,
                    SeamFaceName: seamFace.name,
                    StartMonth: currentContext.StartMonth,
                    EndMonth: currentContext.EndMonth,
                    ProcessName: currentContext.ProcessName,
                    TechnologyName: currentContext.TechnologyName,
                    HardnessName: currentContext.HardnessName,
                    PowerName: currentContext.PowerName,
                    LongwallParametersName: currentContext.LongwallParametersName,
                    CuttingThicknessName: currentContext.CuttingThicknessName);

                if (!aggregates.TryGetValue(key, out var aggregate))
                {
                    importErrors.Add(
                        $"Không tìm thấy dòng thông số cho mã định mức vật liệu '{materialCode}' trước dòng {row}.");
                    continue;
                }

                if (isOtherMaterial)
                {
                    aggregate.AddOtherMaterial(norm);
                    continue;
                }

                if (assignment == null || material == null)
                {
                    importErrors.Add($"Dòng dữ liệu vật tư không hợp lệ ở dòng {row}.");
                    continue;
                }

                try
                {
                    aggregate.AddAssignmentCost(
                        assignment.Id,
                        material.Id,
                        norm,
                        assignment.Display,
                        material.Display);
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

    private static IReadOnlyDictionary<string, MaterialLookupItem> BuildMaterialLookup(
        IEnumerable<Domain.Entities.Index.Material> materials)
    {
        var lookup = new Dictionary<string, MaterialLookupItem>(StringComparer.OrdinalIgnoreCase);

        foreach (var material in materials)
        {
            var code = material.Code?.Value?.Trim() ?? string.Empty;
            var name = material.Name?.Trim() ?? string.Empty;
            var display = BuildMaterialDisplay(code, name);
            var assignmentCodeIds = material.AssignmentCodeMaterials
                .Select(link => link.AssignmentCodeId)
                .Distinct()
                .ToHashSet();
            var item = new MaterialLookupItem(material.Id, display, assignmentCodeIds);

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

    private static string BuildMaterialDisplay(string code, string name)
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

    private static (Guid? powerId, Guid? hardnessId) ResolvePowerAndHardness(
        string powerName,
        string hardnessName,
        IReadOnlyDictionary<string, Guid> powerIdMap,
        IReadOnlyDictionary<string, Guid> hardnessIdMap,
        string code)
    {
        var normalizedPower = NormalizeLookupValue(powerName);
        var normalizedHardness = NormalizeLookupValue(hardnessName);
        var normalizedNone = NormalizeLookupValue(LegacyNoneOptionDisplay);

        var isPowerNone = string.IsNullOrWhiteSpace(normalizedPower) || normalizedPower == normalizedNone;
        var isHardnessNone = string.IsNullOrWhiteSpace(normalizedHardness) || normalizedHardness == normalizedNone;

        if (isPowerNone == isHardnessNone)
        {
            throw new BadRequestException(
                $"Mã '{code}': chỉ một trong hai cột 'Độ kiên cố than đá (f)' hoặc 'Công suất' được để trống.");
        }

        Guid? powerId = null;
        if (!isPowerNone)
        {
            if (!powerIdMap.TryGetValue(normalizedPower, out var mappedPowerId))
            {
                throw new BadRequestException($"Công suất '{powerName}' không tồn tại cho mã '{code}'.");
            }

            powerId = mappedPowerId;
        }

        Guid? hardnessId = null;
        if (!isHardnessNone)
        {
            if (!hardnessIdMap.TryGetValue(normalizedHardness, out var mappedHardnessId))
            {
                throw new BadRequestException($"Độ kiên cố than đá (f) '{hardnessName}' không tồn tại cho mã '{code}'.");
            }

            hardnessId = mappedHardnessId;
        }

        return (powerId, hardnessId);
    }

    private static void ValidateMonthRangeOverlap(
        IEnumerable<LongwallMaterialUnitPriceEntity> existingEntities,
        IEnumerable<LongwallMaterialUnitPriceEntity> addedEntities,
        IEnumerable<LongwallMaterialUnitPriceEntity> deletedEntities)
    {
        var deletedIds = deletedEntities
            .Select(entity => entity.Id)
            .ToHashSet();

        var finalEntities = existingEntities
            .Where(entity => !deletedIds.Contains(entity.Id))
            .Concat(addedEntities)
            .ToList();

        var groupedEntities = finalEntities
            .GroupBy(entity => new MonthRangeOverlapKey(
                entity.LongwallParametersId,
                entity.CuttingThicknessId,
                entity.SeamFaceId,
                entity.PowerId,
                entity.HardnessId));

        foreach (var group in groupedEntities)
        {
            var orderedEntities = group
                .OrderBy(entity => entity.StartMonth)
                .ThenBy(entity => entity.EndMonth)
                .ToList();

            var maxEndMonth = orderedEntities[0].EndMonth;

            for (var index = 1; index < orderedEntities.Count; index++)
            {
                var currentEntity = orderedEntities[index];
                if (maxEndMonth > currentEntity.StartMonth)
                {
                    throw new ConflictException(CustomResponseMessage.MonthRangeOverlap);
                }

                if (currentEntity.EndMonth > maxEndMonth)
                {
                    maxEndMonth = currentEntity.EndMonth;
                }
            }
        }
    }

    private sealed record AssignmentLookupItem(Guid Id, string Display);
    private sealed record MaterialLookupItem(Guid Id, string Display, HashSet<Guid> AssignmentCodeIds);

    private sealed record MonthRangeOverlapKey(
        Guid LongwallParametersId,
        Guid CuttingThicknessId,
        Guid SeamFaceId,
        Guid? PowerId,
        Guid? HardnessId);

    private sealed record LongwallRowContext(
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string TechnologyName,
        string HardnessName,
        string PowerName,
        string LongwallParametersName,
        string CuttingThicknessName,
        IReadOnlyDictionary<string, string> SeamFaceCodeByName);

    private sealed record LongwallRowKey(
        string Code,
        string SeamFaceName,
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string TechnologyName,
        string HardnessName,
        string PowerName,
        string LongwallParametersName,
        string CuttingThicknessName);

    private sealed class ParsedLongwallMaterialUnitPriceRow(
        string code,
        string processName,
        string technologyName,
        string hardnessName,
        string powerName,
        string longwallParametersName,
        string cuttingThicknessName,
        string seamFaceName,
        string startMonth,
        string endMonth)
    {
        private readonly Dictionary<string, AssignmentCostImportItem> _assignmentCostMap = new(StringComparer.OrdinalIgnoreCase);
        private bool _hasOtherMaterial;

        public string Code { get; } = code;
        public string ProcessName { get; } = processName;
        public string TechnologyName { get; } = technologyName;
        public string HardnessName { get; } = hardnessName;
        public string PowerName { get; } = powerName;
        public string LongwallParametersName { get; } = longwallParametersName;
        public string CuttingThicknessName { get; } = cuttingThicknessName;
        public string SeamFaceName { get; } = seamFaceName;
        public string StartMonth { get; } = startMonth;
        public string EndMonth { get; } = endMonth;
        public double OtherMaterialValue { get; private set; }
        public IReadOnlyCollection<AssignmentCostImportItem> AssignmentCosts => _assignmentCostMap.Values;

        public void AddOtherMaterial(double amount)
        {
            if (_hasOtherMaterial)
            {
                throw new BadRequestException(
                    $"VTK - Vật tư khác bị trùng cho mã định mức vật liệu '{Code}'.");
            }
            _hasOtherMaterial = true;
            OtherMaterialValue += amount;
        }

        public void AddAssignmentCost(
            Guid assignmentCodeId,
            Guid materialId,
            double norm,
            string assignmentDisplay,
            string materialDisplay)
        {
            var key = $"{assignmentCodeId}:{materialId}";
            if (_assignmentCostMap.ContainsKey(key))
            {
                throw new BadRequestException(
                    $"Dòng vật tư '{materialDisplay}' trong nhóm '{assignmentDisplay}' bị trùng cho mã định mức vật liệu '{Code}'.");
            }

            _assignmentCostMap[key] = new AssignmentCostImportItem(assignmentCodeId, materialId, norm);
        }
    }

    private sealed record AssignmentCostImportItem(Guid AssignmentCodeId, Guid MaterialId, double Norm);
}
