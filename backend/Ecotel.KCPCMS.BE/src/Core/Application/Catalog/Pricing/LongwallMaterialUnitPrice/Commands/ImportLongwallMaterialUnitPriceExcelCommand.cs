using System.Globalization;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LongwallMaterialUnitPrice;
using ClosedXML.Excel;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LongwallMaterialUnitPriceEntity = Domain.Entities.Pricing.MaterialUnitPrice.LongwallMaterialUnitPrice;

namespace Application.Catalog.Pricing.LongwallMaterialUnitPrice.Commands;

public record ImportLongwallMaterialUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportLongwallMaterialUnitPriceExcelCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<ImportLongwallMaterialUnitPriceExcelCommand, bool>
{
    private readonly IWriteRepository<LongwallMaterialUnitPriceEntity> _materialUnitPriceRepository = unitOfWork.GetRepository<LongwallMaterialUnitPriceEntity>();
    private readonly IWriteRepository<ProductionProcess> _processRepository = unitOfWork.GetRepository<ProductionProcess>();
    private readonly IWriteRepository<LongwallParameters> _longwallParametersRepository = unitOfWork.GetRepository<LongwallParameters>();
    private readonly IWriteRepository<CuttingThickness> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThickness>();
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();

    public async Task<bool> Handle(ImportLongwallMaterialUnitPriceExcelCommand request, CancellationToken cancellationToken)
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

        if (!(await CheckExistedReferences(dtos)))
        {
            throw new BadRequestException("Tồn tại dữ liệu tham chiếu không hợp lệ.");
        }

        var processes = await _processRepository.GetAllAsync(disableTracking: true);
        var longwallParameters = await _longwallParametersRepository.GetAllAsync(disableTracking: true);
        var cuttingThicknesses = await _cuttingThicknessRepository.GetAllAsync(disableTracking: true);
        var seamFaces = await _seamFaceRepository.GetAllAsync(disableTracking: true);
        var technologies = await _technologyRepository.GetAllAsync(disableTracking: true);

        var processIdMap = processes.ToDictionary(p => p.Name, p => p.Id);
        var longwallParametersIdMap = longwallParameters.ToDictionary(l => $"{l.Llc}-{l.Lkc}-{l.Mk}", l => l.Id);
        var cuttingThicknessIdMap = cuttingThicknesses.ToDictionary(c => c.Value, c => c.Id);
        var seamFaceIdMap = seamFaces.ToDictionary(s => s.Value, s => s.Id);
        var technologyIdMap = technologies.ToDictionary(t => t.Value, t => t.Id);

        var dbEntities = await _materialUnitPriceRepository.GetAllAsync(
            include: e => e
                .Include(e => e.Code)
                .Include(e => e.ProductionProcess)
                .Include(e => e.LongwallParameters)
                .Include(e => e.CuttingThickness)
                .Include(e => e.SeamFace)
                .Include(e => e.Technology),
            disableTracking: true);

        var dbLookup = new Dictionary<LongwallLookupKey, LongwallMaterialUnitPriceEntity>();
        foreach (var entity in dbEntities)
        {
            var key = CreateLookupKey(entity);
            if (!dbLookup.ContainsKey(key))
            {
                dbLookup[key] = entity;
            }
        }

        var matchedKeys = new HashSet<LongwallLookupKey>();
        var updateList = new List<LongwallMaterialUnitPriceEntity>();
        var addList = new List<LongwallMaterialUnitPriceEntity>();

        foreach (var dto in dtos)
        {
            var key = CreateLookupKey(dto);
            processIdMap.TryGetValue(dto.ProcessName, out var processId);
            longwallParametersIdMap.TryGetValue(dto.LongwallParametersName, out var longwallParametersId);
            cuttingThicknessIdMap.TryGetValue(dto.CuttingThicknessName, out var cuttingThicknessId);
            seamFaceIdMap.TryGetValue(dto.SeamFaceName, out var seamFaceId);

            Guid? technologyId = null;
            if (!string.IsNullOrWhiteSpace(dto.TechnologyName))
            {
                technologyIdMap.TryGetValue(dto.TechnologyName, out var techId);
                technologyId = techId;
            }

            var startMonth = ParseMonthYear(dto.StartMonth);
            var endMonth = ParseMonthYear(dto.EndMonth);

            if (dbLookup.TryGetValue(key, out var existing))
            {
                existing.Update(
                    dto.Code,
                    processId,
                    longwallParametersId,
                    cuttingThicknessId,
                    seamFaceId,
                    technologyId,
                    startMonth,
                    endMonth,
                    dto.TotalPrice);
                updateList.Add(existing);
                matchedKeys.Add(key);
            }
            else
            {
                var newEntity = LongwallMaterialUnitPriceEntity.Create(
                    dto.Code,
                    processId,
                    longwallParametersId,
                    cuttingThicknessId,
                    seamFaceId,
                    technologyId,
                    startMonth,
                    endMonth,
                    dto.TotalPrice);
                addList.Add(newEntity);
            }
        }

        var deleteList = dbEntities
            .Where(entity => !matchedKeys.Contains(CreateLookupKey(entity)))
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
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<bool> CheckExistedReferences(List<LongwallMaterialUnitPriceExcelDto> dtoList)
    {
        var dbProcessNames = (await _processRepository.GetAllAsync(disableTracking: true))
            .Select(p => p.Name.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbLongwallParametersNames = (await _longwallParametersRepository.GetAllAsync(disableTracking: true))
            .Select(l => $"{l.Llc}-{l.Lkc}-{l.Mk}".Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbCuttingThicknessNames = (await _cuttingThicknessRepository.GetAllAsync(disableTracking: true))
            .Select(c => c.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbSeamFaceNames = (await _seamFaceRepository.GetAllAsync(disableTracking: true))
            .Select(s => s.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var dbTechnologyNames = (await _technologyRepository.GetAllAsync(disableTracking: true))
            .Select(t => t.Value.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var excelProcesses = dtoList.Select(d => d.ProcessName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelLongwallParameters = dtoList.Select(d => d.LongwallParametersName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelCuttingThicknesses = dtoList.Select(d => d.CuttingThicknessName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelSeamFaces = dtoList.Select(d => d.SeamFaceName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();
        var excelTechnologies = dtoList.Select(d => d.TechnologyName?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();

        return excelProcesses.All(name => dbProcessNames.Contains(name))
            && excelLongwallParameters.All(name => dbLongwallParametersNames.Contains(name))
            && excelCuttingThicknesses.All(name => dbCuttingThicknessNames.Contains(name))
            && excelSeamFaces.All(name => dbSeamFaceNames.Contains(name))
            && excelTechnologies.All(name => dbTechnologyNames.Contains(name));
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            var now = DateTime.Now;
            return new DateOnly(now.Year, now.Month, 1);
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

    private static List<LongwallMaterialUnitPriceExcelDto> ParseFromCustomTemplate(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheet(1);

        const int idCol = 1;
        const int startMonthCol = 2;
        const int endMonthCol = 3;
        const int processCol = 4;
        const int technologyCol = 5;
        const int longwallParametersCol = 6;
        const int cuttingThicknessCol = 7;
        const int seamFaceStartCol = 8;

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var lastCol = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        if (lastRow < 4 || lastCol < seamFaceStartCol)
        {
            return new List<LongwallMaterialUnitPriceExcelDto>();
        }

        var seamFacePositions = new List<(int dmCol, int ttCol, string name)>();
        for (int col = seamFaceStartCol; col <= lastCol; col += 2)
        {
            var seamFaceName = worksheet.Cell(1, col).GetString().Trim();
            if (string.IsNullOrWhiteSpace(seamFaceName))
            {
                continue;
            }

            if (col + 1 > lastCol)
            {
                continue;
            }

            seamFacePositions.Add((col, col + 1, seamFaceName));
        }

        var currentMonth = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1).ToString("MM/yyyy");
        var result = new List<LongwallMaterialUnitPriceExcelDto>();

        for (int row = 4; row <= lastRow; row++)
        {
            var processName = worksheet.Cell(row, processCol).GetString().Trim();
            var technologyName = worksheet.Cell(row, technologyCol).GetString().Trim();
            var longwallParametersName = worksheet.Cell(row, longwallParametersCol).GetString().Trim();
            var cuttingThicknessName = worksheet.Cell(row, cuttingThicknessCol).GetString().Trim();

            var hasValue = seamFacePositions.Any(position => !worksheet.Cell(row, position.ttCol).IsEmpty());
            if (string.IsNullOrWhiteSpace(processName)
                && string.IsNullOrWhiteSpace(technologyName)
                && string.IsNullOrWhiteSpace(longwallParametersName)
                && string.IsNullOrWhiteSpace(cuttingThicknessName)
                && !hasValue)
            {
                continue;
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
            if (string.IsNullOrWhiteSpace(endMonth))
            {
                endMonth = currentMonth;
            }

            var fallbackCode = $"LWL-{Guid.NewGuid():N}"[..12].ToUpper();

            foreach (var (dmCol, ttCol, seamFaceName) in seamFacePositions)
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

                var codeValue = worksheet.Cell(row, dmCol).GetString().Trim();
                if (string.IsNullOrWhiteSpace(codeValue))
                {
                    codeValue = fallbackCode;
                }

                result.Add(new LongwallMaterialUnitPriceExcelDto
                {
                    Id = id,
                    Code = codeValue,
                    ProcessName = processName,
                    TechnologyName = technologyName,
                    LongwallParametersName = longwallParametersName,
                    CuttingThicknessName = cuttingThicknessName,
                    SeamFaceName = seamFaceName,
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

    private static LongwallLookupKey CreateLookupKey(LongwallMaterialUnitPriceExcelDto dto)
    {
        return new(
            StartMonth: dto.StartMonth.Trim(),
            EndMonth: dto.EndMonth.Trim(),
            ProcessName: dto.ProcessName.Trim(),
            TechnologyName: dto.TechnologyName.Trim(),
            LongwallParametersName: dto.LongwallParametersName.Trim(),
            CuttingThicknessName: dto.CuttingThicknessName.Trim(),
            SeamFaceName: dto.SeamFaceName.Trim());
    }

    private static LongwallLookupKey CreateLookupKey(LongwallMaterialUnitPriceEntity entity)
    {
        return new(
            StartMonth: entity.StartMonth.ToString("MM/yyyy"),
            EndMonth: entity.EndMonth.ToString("MM/yyyy"),
            ProcessName: entity.ProductionProcess?.Name?.Trim() ?? string.Empty,
            TechnologyName: entity.Technology?.Value?.Trim() ?? string.Empty,
            LongwallParametersName: entity.LongwallParameters != null ? $"{entity.LongwallParameters.Llc}-{entity.LongwallParameters.Lkc}-{entity.LongwallParameters.Mk}" : string.Empty,
            CuttingThicknessName: entity.CuttingThickness?.Value?.Trim() ?? string.Empty,
            SeamFaceName: entity.SeamFace?.Value?.Trim() ?? string.Empty);
    }

    private sealed record LongwallLookupKey(
        string StartMonth,
        string EndMonth,
        string ProcessName,
        string TechnologyName,
        string LongwallParametersName,
        string CuttingThicknessName,
        string SeamFaceName);
}
