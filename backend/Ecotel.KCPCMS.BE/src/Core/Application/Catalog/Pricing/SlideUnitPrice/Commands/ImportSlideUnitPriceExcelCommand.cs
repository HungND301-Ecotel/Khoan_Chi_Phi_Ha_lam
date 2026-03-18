using System.Globalization;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.SlideUnitPrice.Commands;

public record ImportSlideUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportSlideUnitPriceExcelCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ImportSlideUnitPriceExcelCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Pricing.SlideUnitPrice> _slideUnitPriceRepository = unitOfWork.GetRepository<Domain.Entities.Pricing.SlideUnitPrice>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    private readonly IWriteRepository<Passport> _passportRepository = unitOfWork.GetRepository<Passport>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    private readonly IWriteRepository<Material> _materialRepository = unitOfWork.GetRepository<Material>();
    public async Task<bool> Handle(ImportSlideUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var slideUnitPrices = ParseFromCustomTemplate(stream);

        if (!slideUnitPrices.Any())
        {
            throw new BadRequestException("Không có dữ liệu hợp lệ để import.");
        }

        if (!(await CheckExistedReferences(slideUnitPrices)))
        {
            throw new BadRequestException("Tồn tại dữ liệu tham chiếu không hợp lệ.");
        }

        // Map data to Entity Model
        var processGroups = await _processGroupRepository.GetAllAsync(disableTracking: true);
        var passports = await _passportRepository.GetAllAsync(disableTracking: true);
        var hardnesses = await _hardnessRepository.GetAllAsync(disableTracking: true);
        var materials = await _materialRepository.GetAllAsync(
            include: m => m.Include(m => m.Code),
            disableTracking: true);

        var processGroupIdMap = processGroups.ToDictionary(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);
        var passportIdMap = passports.ToDictionary(p => $"H/c {p.Name}; {p.Sd}; {p.Sc}", p => p.Id, StringComparer.OrdinalIgnoreCase);
        var hardnessIdMap = hardnesses.ToDictionary(h => h.Value, h => h.Id, StringComparer.OrdinalIgnoreCase);
        var materialIdMap = materials
            .Where(m => !string.IsNullOrWhiteSpace(m.Code?.Value))
            .GroupBy(m => m.Code!.Value.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        var excelItems = slideUnitPrices.Select(d =>
        {
            processGroupIdMap.TryGetValue(d.ProcessGroupName, out var processGroupId);
            passportIdMap.TryGetValue(d.PassportName, out var passportId);
            hardnessIdMap.TryGetValue(d.HardnessName, out var hardnessId);

            var startMonth = ParseMonthYear(d.StartMonth);
            var endMonth = ParseMonthYear(d.EndMonth);

            var assignmentCodes = d.MaterialAmounts
                .Where(x => materialIdMap.ContainsKey(x.Key))
                .Select(x => SlideUnitPriceAssignmentCode.Create(materialIdMap[x.Key], x.Value))
                .ToList();

            var entity = Domain.Entities.Pricing.SlideUnitPrice.Create(
                d.Code,
                processGroupId,
                hardnessId,
                passportId,
                startMonth,
                endMonth,
                assignmentCodes);

            return new
            {
                Dto = d,
                Entity = entity
            };
        }).ToList();

        // Fetch with includes to create lookup keys
        var dbEntitiesForLookup = await _slideUnitPriceRepository.GetAllAsync(
            include: a => a
                .Include(a => a.Code!)
                .Include(a => a.ProcessGroup)
                .Include(a => a.Passport)
                .Include(a => a.Hardness)
                .Include(a => a.SlideUnitPriceAssignmentCodes),
            disableTracking: true);

        var dbLookup = new Dictionary<SlideUnitPriceLookupKey, Guid>();
        var idToLookupKeyMap = new Dictionary<Guid, SlideUnitPriceLookupKey>();
        foreach (var entity in dbEntitiesForLookup)
        {
            var key = CreateLookupKey(entity);
            if (!dbLookup.ContainsKey(key))
            {
                dbLookup[key] = entity.Id;
                idToLookupKeyMap[entity.Id] = key;
            }
        }

        // Fetch without includes for update/delete (with tracking enabled)
        var dbEntities = await _slideUnitPriceRepository.GetAllAsync(disableTracking: false);

        var matchedKeys = new HashSet<SlideUnitPriceLookupKey>();
        var updateList = new List<Domain.Entities.Pricing.SlideUnitPrice>();
        var addList = new List<Domain.Entities.Pricing.SlideUnitPrice>();

        foreach (var item in excelItems)
        {
            var excelEntity = item.Entity;
            var key = CreateLookupKey(item.Dto);
            if (dbLookup.TryGetValue(key, out var dbEntityId))
            {
                var entityToUpdate = dbEntities.FirstOrDefault(e => e.Id == dbEntityId);
                if (entityToUpdate != null)
                {
                    // Recreate assignment codes from DTO to avoid tracking conflicts
                    var newAssignmentCodes = item.Dto.MaterialAmounts
                        .Where(x => materialIdMap.ContainsKey(x.Key))
                        .Select(x => SlideUnitPriceAssignmentCode.Create(materialIdMap[x.Key], x.Value))
                        .ToList();

                    entityToUpdate.Update(
                        excelEntity.Code.Value,
                        excelEntity.ProcessGroupId,
                        excelEntity.HardnessId,
                        excelEntity.PassportId,
                        excelEntity.StartMonth,
                        excelEntity.EndMonth,
                        newAssignmentCodes);

                    updateList.Add(entityToUpdate);
                    matchedKeys.Add(key);
                }
            }
            else
            {
                addList.Add(excelEntity);
            }
        }

        var deleteList = dbEntities
            .Where(entity => !matchedKeys.Contains(idToLookupKeyMap.GetValueOrDefault(entity.Id, CreateLookupKey(entity))))
            .ToList();

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _slideUnitPriceRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _slideUnitPriceRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _slideUnitPriceRepository.Update(updateList);
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

    private static SlideUnitPriceLookupKey CreateLookupKey(Domain.Entities.Pricing.SlideUnitPrice entity)
    {
        return new(
            StartMonth: entity.StartMonth.ToString("MM/yyyy"),
            ProcessGroupName: entity.ProcessGroup?.Name?.Trim() ?? string.Empty,
            PassportName: entity.Passport != null
                ? $"H/c {entity.Passport.Name}; {entity.Passport.Sd}; {entity.Passport.Sc}".Trim()
                : string.Empty,
            HardnessName: entity.Hardness?.Value?.Trim() ?? string.Empty);
    }

    private static SlideUnitPriceLookupKey CreateLookupKey(SlideUnitPriceImportDto dto)
    {
        return new(
            StartMonth: ParseMonthYear(dto.StartMonth).ToString("MM/yyyy"),
            ProcessGroupName: dto.ProcessGroupName.Trim(),
            PassportName: dto.PassportName.Trim(),
            HardnessName: dto.HardnessName.Trim());
    }

    private sealed record SlideUnitPriceLookupKey(
        string StartMonth,
        string ProcessGroupName,
        string PassportName,
        string HardnessName);

    private static List<SlideUnitPriceImportDto> ParseFromCustomTemplate(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);

        const int idCol = 1;
        const int startMonthCol = 2;
        const int endMonthCol = 3;
        const int processGroupCol = 4;
        const int hardnessCol = 5;
        const int materialCodeCol = 6;
        const int passportStartCol = 7;

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 3 || lastCol < passportStartCol)
        {
            return new List<SlideUnitPriceImportDto>();
        }

        var passportPositions = new List<(int dmCol, int ttCol, string name)>();
        for (int col = passportStartCol; col <= lastCol; col += 2)
        {
            var passportName = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(passportName) || col + 1 > lastCol)
            {
                continue;
            }

            passportPositions.Add((col, col + 1, passportName));
        }

        if (!passportPositions.Any())
        {
            return new List<SlideUnitPriceImportDto>();
        }

        var result = new List<SlideUnitPriceImportDto>();
        var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("MM/yyyy");
        var row = 3;

        while (row <= lastRow)
        {
            var activePassports = passportPositions
                .Where(p => !string.IsNullOrWhiteSpace(worksheet.Cell(row, p.dmCol).GetString().Trim()))
                .ToList();

            if (!activePassports.Any())
            {
                row++;
                continue;
            }

            var blockEndRow = row;
            while (blockEndRow + 1 <= lastRow)
            {
                var nextRowHasCode = passportPositions
                    .Any(p => !string.IsNullOrWhiteSpace(worksheet.Cell(blockEndRow + 1, p.dmCol).GetString().Trim()));

                if (nextRowHasCode)
                {
                    break;
                }

                blockEndRow++;
            }

            var id = Guid.Empty;
            var idText = worksheet.Cell(row, idCol).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(idText) && Guid.TryParse(idText, out var parsedId))
            {
                id = parsedId;
            }

            var startMonth = worksheet.Cell(row, startMonthCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(startMonth))
            {
                startMonth = currentMonth;
            }

            var endMonth = worksheet.Cell(row, endMonthCol).GetString().Trim();
            if (string.IsNullOrWhiteSpace(startMonth))
            {
                endMonth = startMonth;
            }

            var processGroupName = worksheet.Cell(row, processGroupCol).GetString().Trim();
            var hardnessName = worksheet.Cell(row, hardnessCol).GetString().Trim();

            foreach (var passport in activePassports)
            {
                var codeValue = worksheet.Cell(row, passport.dmCol).GetString().Trim();
                if (string.IsNullOrWhiteSpace(codeValue))
                {
                    continue;
                }

                var materialAmounts = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                for (var materialRow = row; materialRow <= blockEndRow; materialRow++)
                {
                    var materialCode = worksheet.Cell(materialRow, materialCodeCol).GetString().Trim();
                    if (string.IsNullOrWhiteSpace(materialCode))
                    {
                        continue;
                    }

                    var ttCell = worksheet.Cell(materialRow, passport.ttCol);
                    if (ttCell.IsEmpty())
                    {
                        continue;
                    }

                    if (!TryParseDouble(ttCell, out var amount))
                    {
                        throw new BadRequestException($"Giá trị Tổng tiền không hợp lệ tại dòng {materialRow}, cột {passport.ttCol}.");
                    }

                    materialAmounts[materialCode] = amount;
                }

                result.Add(new SlideUnitPriceImportDto
                {
                    Id = id,
                    Code = codeValue,
                    ProcessGroupName = processGroupName,
                    PassportName = passport.name,
                    HardnessName = hardnessName,
                    StartMonth = startMonth,
                    EndMonth = endMonth,
                    MaterialAmounts = materialAmounts
                });
            }

            row = blockEndRow + 1;
        }

        return result;
    }

    private async Task<bool> CheckExistedReferences(List<SlideUnitPriceImportDto> dtoList)
    {
        var dbProcessGroupNames = (await _processGroupRepository.GetAllAsync(disableTracking: true))
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

        var dbMaterialCodes = (await _materialRepository.GetAllAsync(
                include: m => m.Include(m => m.Code),
                disableTracking: true))
            .Select(m => m.Code?.Value?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var excelProcessGroups = dtoList.Select(d => d.ProcessGroupName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelPassports = dtoList.Select(d => d.PassportName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelHardnesses = dtoList.Select(d => d.HardnessName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelMaterialCodes = dtoList
            .SelectMany(d => d.MaterialAmounts.Keys)
            .Select(code => code.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return excelProcessGroups.All(name => dbProcessGroupNames.Contains(name))
            && excelPassports.All(name => dbPassportNames.Contains(name))
            && excelHardnesses.All(name => dbHardnessNames.Contains(name))
            && excelMaterialCodes.All(code => dbMaterialCodes.Contains(code));
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            return DateOnly.MinValue;
        }

        if (DateOnly.TryParseExact(monthYear, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out var result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(monthYear, "M/yyyy", null, System.Globalization.DateTimeStyles.None, out result))
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
}

internal class SlideUnitPriceImportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ProcessGroupName { get; set; } = string.Empty;
    public string PassportName { get; set; } = string.Empty;
    public string HardnessName { get; set; } = string.Empty;
    public string StartMonth { get; set; } = string.Empty;
    public string EndMonth { get; set; } = string.Empty;
    public Dictionary<string, double> MaterialAmounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
