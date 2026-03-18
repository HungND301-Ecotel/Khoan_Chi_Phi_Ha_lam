using System.Globalization;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MaterialUnitPrice;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MaterialUnitPrice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaterialUnitPrice.Commands;

public record ImportMaterialUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportMaterialUnitPriceExcelCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ImportMaterialUnitPriceExcelCommand, bool>
{
    private readonly IWriteRepository<TunnelExcavationMaterialUnitPrice> _materialUnitPriceRepository = unitOfWork.GetRepository<TunnelExcavationMaterialUnitPrice>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    private readonly IWriteRepository<SupportStep> _supportStepRepository = unitOfWork.GetRepository<SupportStep>();

    public async Task<bool> Handle(ImportMaterialUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = ParseFromCustomTemplate(stream);

        if (!dtos.Any())
        {
            throw new BadRequestException("Không có dữ liệu hợp lệ để import.");
        }

        // Check trùng key trong chính file Excel
        var duplicateKeys = dtos
            .GroupBy(CreateLookupKey)
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var duplicateCodesInKey = g
                    .Select(x => x.Code?.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                return $"{g.Key.StartMonth} - {g.Key.EndMonth} | {g.Key.ProcessName} | {g.Key.PassportName} | {g.Key.HardnessName} | {g.Key.InsertItemName} | {g.Key.SupportStepName} | Codes: {string.Join(", ", duplicateCodesInKey)}";
            })
            .ToList();

        if (duplicateKeys.Any())
        {
            throw new BadRequestException($"File Excel có dữ liệu trùng key: {string.Join("; ", duplicateKeys)}");
        }

        var duplicateCodes = dtos
            .Select(d => d.Code?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .GroupBy(code => code!, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateCodes.Any())
        {
            throw new BadRequestException($"File Excel có mã định mức vật liệu bị trùng: {string.Join("; ", duplicateCodes)}");
        }

        if (!(await CheckExistedReferences(dtos)))
        {
            throw new BadRequestException("Tồn tại dữ liệu tham chiếu không hợp lệ.");
        }

        // Map data to Entity Model
        var processes = await _processRepository.GetAllAsync(disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(disableTracking: true);
        var insertItems = await _insertItemRepository.GetAllAsync(disableTracking: true);
        var supportSteps = await _supportStepRepository.GetAllAsync(disableTracking: true);

        var processIdMap = processes.ToDictionary(p => p.Name, p => p.Id);
        var passportIdMap = passports.ToDictionary(p => $"H/c {p.Name}; {p.Sd}; {p.Sc}", p => p.Id,
                            StringComparer.OrdinalIgnoreCase);
        var hardnessIdMap = hardnesses.ToDictionary(h => h.Value, h => h.Id);
        var insertItemIdMap = insertItems.ToDictionary(i => i.Value, i => i.Id);
        var supportStepIdMap = supportSteps.ToDictionary(s => s.Value, s => s.Id);

        var dbEntities = await _materialUnitPriceRepository.GetAllAsync(
            include: a => a.Include(a => a.Code!),
            disableTracking: true);

        var dbCodeLookup = new Dictionary<string, TunnelExcavationMaterialUnitPrice>(StringComparer.OrdinalIgnoreCase);
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
        var updateList = new List<TunnelExcavationMaterialUnitPrice>();
        var addList = new List<TunnelExcavationMaterialUnitPrice>();

        foreach (var dto in dtos)
        {
            var code = dto.Code.Trim();
            processIdMap.TryGetValue(dto.ProcessName, out var processId);
            passportIdMap.TryGetValue(dto.PassportName, out var passportId);
            hardnessIdMap.TryGetValue(dto.HardnessName, out var hardnessId);
            insertItemIdMap.TryGetValue(dto.InsertItemName, out var insertItemId);
            supportStepIdMap.TryGetValue(dto.SupportStepName, out var supportStepId);

            var startMonth = ParseMonthYear(dto.StartMonth);
            var endMonth = ParseMonthYear(dto.EndMonth);

            if (dbCodeLookup.TryGetValue(code, out var existingEntity))
            {
                existingEntity.Update(
                    code,
                    processId,
                    passportId,
                    hardnessId,
                    insertItemId,
                    supportStepId,
                    existingEntity.TechnologyId,
                    startMonth,
                    endMonth,
                    dto.TotalPrice);
                updateList.Add(existingEntity);
                matchedCodes.Add(code);
            }
            else
            {
                var newEntity = TunnelExcavationMaterialUnitPrice.Create(
                    code,
                    processId,
                    passportId,
                    hardnessId,
                    insertItemId,
                    supportStepId,
                    null,
                    startMonth,
                    endMonth,
                    dto.TotalPrice);
                addList.Add(newEntity);
                matchedCodes.Add(code);
            }
        }

        var deleteList = dbEntities
            .Where(entity =>
            {
                var code = entity.Code?.Value?.Trim();
                return !string.IsNullOrWhiteSpace(code) && !matchedCodes.Contains(code);
            })
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
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
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private async Task<bool> CheckExistedReferences(List<MaterialUnitPriceExcelDto> dtoList)
    {
        var dbProcessNames = (await _processRepository.GetAllAsync(disableTracking: true))
            .Select(p => p.Name.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbPassportNames = (await _passportRepository.GetAllAsync(disableTracking: true))
            .Select(p => $"H/c {p.Name}; {p.Sd}; {p.Sc}".Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbHardnessNames = (await _hardnessRepository.GetAllAsync(disableTracking: true))
            .Select(h => h.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbInsertItemNames = (await _insertItemRepository.GetAllAsync(disableTracking: true))
            .Select(i => i.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbSupportStepNames = (await _supportStepRepository.GetAllAsync(disableTracking: true))
            .Select(s => s.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var excelProcesses = dtoList.Select(d => d.ProcessName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelPassports = dtoList.Select(d => d.PassportName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelHardnesses = dtoList.Select(d => d.HardnessName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelInsertItems = dtoList.Select(d => d.InsertItemName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelSupportSteps = dtoList.Select(d => d.SupportStepName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();

        return excelProcesses.All(name => dbProcessNames.Contains(name))
            && excelPassports.All(name => dbPassportNames.Contains(name))
            && excelHardnesses.All(name => dbHardnessNames.Contains(name))
            && excelInsertItems.All(name => dbInsertItemNames.Contains(name))
            && excelSupportSteps.All(name => dbSupportStepNames.Contains(name));
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

    private static List<MaterialUnitPriceExcelDto> ParseFromCustomTemplate(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        const int startMonthCol = 1;
        const int endMonthCol = 2;
        const int processCol = 3;
        const int hardnessCol = 4;
        const int insertItemCol = 5;
        const int supportStepCol = 6;
        const int passportStartCol = 7;

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 3 || lastCol < passportStartCol)
        {
            return new List<MaterialUnitPriceExcelDto>();
        }

        var passportPositions = new List<(int dmCol, int ttCol, string name)>();
        for (int col = passportStartCol; col <= lastCol; col += 2)
        {
            var passportName = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(passportName))
            {
                continue;
            }

            if (col + 1 > lastCol)
            {
                continue;
            }

            passportPositions.Add((col, col + 1, passportName));
        }

        var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("MM/yyyy");
        var result = new List<MaterialUnitPriceExcelDto>();

        for (int row = 3; row <= lastRow; row++)
        {
            var processName = worksheet.Cell(row, processCol).GetString().Trim();
            var hardnessName = worksheet.Cell(row, hardnessCol).GetString().Trim();
            var insertItemName = worksheet.Cell(row, insertItemCol).GetString().Trim();
            var supportStepName = worksheet.Cell(row, supportStepCol).GetString().Trim();

            var hasTtValue = passportPositions.Any(position => !worksheet.Cell(row, position.ttCol).IsEmpty());

            if (string.IsNullOrWhiteSpace(processName)
                && string.IsNullOrWhiteSpace(hardnessName)
                && string.IsNullOrWhiteSpace(insertItemName)
                && string.IsNullOrWhiteSpace(supportStepName)
                && !hasTtValue)
            {
                continue;
            }

            var startMonth = worksheet.Cell(row, startMonthCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(startMonth))
            {
                startMonth = currentMonth;
            }

            var endMonth = worksheet.Cell(row, endMonthCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(endMonth))
            {
                endMonth = startMonth;
            }

            foreach (var (dmCol, ttCol, passportName) in passportPositions)
            {
                var ttCell = worksheet.Cell(row, ttCol);
                if (ttCell.IsEmpty())
                {
                    continue;
                }

                if (!TryParseDouble(ttCell, out var totalPrice))
                {
                    throw new BadRequestException($"Giá trị TT không hợp lệ tại dòng {row}, cột {ttCol}.");
                }

                var code = worksheet.Cell(row, dmCol).GetString().Trim();
                if (string.IsNullOrWhiteSpace(code))
                {
                    code = $"MUP-{Guid.NewGuid():N}"[..12].ToUpper();
                }

                result.Add(new MaterialUnitPriceExcelDto
                {
                    Code = code,
                    ProcessName = processName,
                    PassportName = passportName,
                    HardnessName = hardnessName,
                    InsertItemName = insertItemName,
                    SupportStepName = supportStepName,
                    StartMonth = startMonth,
                    EndMonth = endMonth,
                    TotalPrice = totalPrice
                });
            }
        }

        return result;
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

    private static MaterialUnitPriceLookupKey CreateLookupKey(MaterialUnitPriceExcelDto dto)
    {
        return new(
            StartMonth: dto.StartMonth.Trim(),
            EndMonth: dto.EndMonth.Trim(),
            ProcessName: NormalizeString(dto.ProcessName),
            PassportName: dto.PassportName.Trim(),
            HardnessName: dto.HardnessName.Trim(),
            InsertItemName: dto.InsertItemName.Trim(),
            SupportStepName: dto.SupportStepName.Trim());
    }

    private sealed record MaterialUnitPriceLookupKey(
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string PassportName,
        string HardnessName,
        string InsertItemName,
        string SupportStepName);

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }
        // Xóa dấu cách thừa ở giữa và 2 đầu
        return System.Text.RegularExpressions.Regex.Replace(input.Trim(), @"\s+", " ");
    }
}
