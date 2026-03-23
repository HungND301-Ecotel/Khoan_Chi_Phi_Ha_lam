using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using ClosedXML.Excel;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.MaintainUnitPriceEquipment.Commands;

public record ImportTunnelMaintainUnitPriceEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTunnelMaintainUnitPriceEquipmentExcelCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ImportTunnelMaintainUnitPriceEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<MaintainUnitPrice> _repository = unitOfWork.GetRepository<MaintainUnitPrice>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();

    public async Task<bool> Handle(ImportTunnelMaintainUnitPriceEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var maintainUnitPrices = ParseTransposedExcel(stream);

        if (!(await CheckExistedReferences(maintainUnitPrices)))
        {
            throw new BadRequestException("Tồn tại dữ liệu tham chiếu không hợp lệ.");
        }

        // Map data
        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e.Include(e => e.Code),
            disableTracking: true);

        var equipmentIdMap = equipments.Where(e => e.Code != null).ToDictionary(e => e.Code!.Value, e => e.Id);

        var excelItems = maintainUnitPrices.Select(d =>
        {
            equipmentIdMap.TryGetValue(d.EquipmentCode, out var equipmentId);
            var startMonth = ParseMonthYear(d.StartMonth);
            var endMonth = ParseMonthYear(d.EndMonth); // 👈 parse từ DTO thay vì gán = startMonth

            // Map parts using Part Id directly (no need to query Part repository)
            var maintainUnitPriceEquipments = d.PartData
                .Where(pd => pd.Value.HasValue)
                .Select(pd => Domain.Entities.Pricing.MaintainUnitPriceEquipment.Create(
                    null,
                    pd.Key, // PartId from Excel
                    pd.Value!.Value.Quantity,
                    pd.Value.Value.AverageMonthlyTunnelProduction))
                .ToList();

            return new
            {
                EquipmentId = equipmentId,
                StartMonth = startMonth,
                EndMonth = endMonth,
                Parts = maintainUnitPriceEquipments,
                IsDeleteRequest = d.IsDeleteRequest
            };
        }).ToList();

        var dbEntities = await _repository.GetAllAsync(
            predicate: m => m.Type == MaintainUnitPriceType.TunnelExcavation,
            include: m => m.Include(m => m.MaintainUnitPriceEquipments),
            disableTracking: false);

        var dbLookup = dbEntities
            .GroupBy(x => new MaintainUnitPriceLookupKey(x.EquipmentId, x.StartMonth))
            .ToDictionary(g => g.Key, g => g.First());

        var deleteList = new List<MaintainUnitPrice>();
        var updateList = new List<MaintainUnitPrice>();
        var addList = new List<MaintainUnitPrice>();
        var processedKeys = new HashSet<MaintainUnitPriceLookupKey>();

        foreach (var excelItem in excelItems)
        {
            var key = new MaintainUnitPriceLookupKey(excelItem.EquipmentId, excelItem.StartMonth);
            if (!processedKeys.Add(key))
            {
                throw new BadRequestException($"Dữ liệu bị trùng cho mã thiết bị và thời gian: {excelItem.StartMonth:MM/yyyy}.");
            }

            if (excelItem.IsDeleteRequest)
            {
                if (dbLookup.TryGetValue(key, out var entityToDelete))
                {
                    deleteList.Add(entityToDelete);
                }

                continue;
            }

            if (dbLookup.TryGetValue(key, out var entityToUpdate))
            {
                var newParts = excelItem.Parts.ToList();
                entityToUpdate.Update(
                    excelItem.EquipmentId,
                    excelItem.StartMonth,
                    excelItem.EndMonth,
                    newParts,
                    entityToUpdate.OtherMaterialValue,
                    entityToUpdate.Type);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(MaintainUnitPrice.Create(
                    excelItem.EquipmentId,
                    excelItem.StartMonth,
                    excelItem.EndMonth,
                    excelItem.Parts,
                    null,
                    MaintainUnitPriceType.TunnelExcavation));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _repository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _repository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _repository.Update(updateList);
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

    private List<TunnelMaintainUnitPriceImportDto> ParseTransposedExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var result = new List<TunnelMaintainUnitPriceImportDto>();

        // Find number of MaintainUnitPrice groups (each takes 3 columns, starting from column E)
        int colCount = 5;
        while (!worksheet.Cell(1, colCount).IsEmpty())
        {
            colCount += 3;
        }
        colCount -= 3; // Adjust to last filled column group

        // Read each MaintainUnitPrice (every 3 columns from E onwards)
        for (int col = 5; col <= colCount; col += 3)
        {
            var startMonth = worksheet.Cell(1, col).GetString(); // Row 1: StartMonth
            var endMonth = worksheet.Cell(2, col).GetString();   // Row 2: EndMonth
            if (string.IsNullOrWhiteSpace(endMonth))
            {
                endMonth = startMonth; // fallback
            }

            var stateMap = new Dictionary<string, TunnelEquipmentImportState>(StringComparer.OrdinalIgnoreCase);

            // Read part data (from row 4 onwards, row 3 is header)
            int row = 4; // 👈 đổi từ 3 thành 4
            var currentEquipmentCode = string.Empty;
            while (!worksheet.Cell(row, 3).IsEmpty()) // Check column C (Part Code) for end of data
            {
                var equipmentCodeCellValue = worksheet.Cell(row, 2).GetString().Trim(); // Column B: Equipment Code
                if (!string.IsNullOrWhiteSpace(equipmentCodeCellValue))
                {
                    currentEquipmentCode = equipmentCodeCellValue;
                }

                if (string.IsNullOrWhiteSpace(currentEquipmentCode))
                {
                    row++;
                    continue;
                }

                if (!stateMap.TryGetValue(currentEquipmentCode, out var state))
                {
                    state = new TunnelEquipmentImportState
                    {
                        Dto = new TunnelMaintainUnitPriceImportDto
                        {
                            EquipmentCode = currentEquipmentCode,
                            StartMonth = startMonth,
                            EndMonth = endMonth, // 👈 gán đúng endMonth
                            PartData = new Dictionary<Guid, TunnelPartEquipmentData?>()
                        }
                    };
                    stateMap[currentEquipmentCode] = state;
                }

                var partIdString = worksheet.Cell(row, 1).GetString(); // Column A: Part Id (hidden)
                var replacementTimeValue = worksheet.Cell(row, col).GetString();
                var quantityValue = worksheet.Cell(row, col + 1).GetString();
                var productionValue = worksheet.Cell(row, col + 2).GetString();

                var hasAnyValue = !string.IsNullOrWhiteSpace(replacementTimeValue)
                    || !string.IsNullOrWhiteSpace(quantityValue)
                    || !string.IsNullOrWhiteSpace(productionValue);

                var hasAllValues = !string.IsNullOrWhiteSpace(replacementTimeValue)
                    && !string.IsNullOrWhiteSpace(quantityValue)
                    && !string.IsNullOrWhiteSpace(productionValue);

                if (hasAnyValue && !hasAllValues)
                {
                    throw new BadRequestException($"Thiếu dữ liệu định mức tại dòng {row}, cụm cột bắt đầu {col}. Vui lòng nhập đủ 3 cột: Định mức thời gian thay thế, Số lượng vật tư, Sản lượng than bình quân tháng.");
                }

                if (!string.IsNullOrWhiteSpace(partIdString) &&
                    Guid.TryParse(partIdString, out var partId))
                {
                    state.TotalParts++;

                    if (!hasAllValues)
                    {
                        row++;
                        continue;
                    }

                    if (decimal.TryParse(replacementTimeValue, out var replacementTime) &&
                        double.TryParse(quantityValue, out var quantity) &&
                        decimal.TryParse(productionValue, out var production))
                    {
                        state.FilledParts++;
                        state.Dto.PartData[partId] = new TunnelPartEquipmentData
                        {
                            Quantity = quantity,
                            AverageMonthlyTunnelProduction = production
                        };
                    }
                    else
                    {
                        throw new BadRequestException($"Dữ liệu số không hợp lệ tại dòng {row}, cụm cột bắt đầu {col}.");
                    }
                }

                row++;
            }

            foreach (var state in stateMap.Values)
            {
                if (state.TotalParts == 0)
                {
                    continue;
                }

                if (state.FilledParts > 0 && state.FilledParts < state.TotalParts)
                {
                    throw new BadRequestException($"Thiết bị {state.Dto.EquipmentCode} chưa nhập đủ 3 thông số cho tất cả phụ tùng.");
                }

                state.Dto.IsDeleteRequest = state.FilledParts == 0;
                result.Add(state.Dto);
            }
        }

        return result;
    }

    private async Task<bool> CheckExistedReferences(List<TunnelMaintainUnitPriceImportDto> dtoList)
    {
        var dbEquipmentCodes = (await _equipmentRepository.GetAllAsync(
                include: e => e.Include(e => e.Code),
                disableTracking: true))
            .Where(e => e.Code != null)
            .Select(e => e.Code!.Value.Trim())
            .ToHashSet();

        var excelEquipmentCodes = dtoList.Select(d => d.EquipmentCode?.Trim()).Where(n => !string.IsNullOrEmpty(n)).Distinct();

        return excelEquipmentCodes.All(code => dbEquipmentCodes.Contains(code));
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
}

internal class TunnelMaintainUnitPriceImportDto
{
    public string EquipmentCode { get; set; } = string.Empty;
    public string StartMonth { get; set; } = string.Empty;
    public string EndMonth { get; set; } = string.Empty;
    public bool IsDeleteRequest { get; set; }
    public Dictionary<Guid, TunnelPartEquipmentData?> PartData { get; set; } = new(); // Key is PartId
}

internal readonly record struct MaintainUnitPriceLookupKey(Guid EquipmentId, DateOnly StartMonth);

internal struct TunnelPartEquipmentData
{
    public double Quantity { get; set; }
    public decimal AverageMonthlyTunnelProduction { get; set; }
}

internal class TunnelEquipmentImportState
{
    public required TunnelMaintainUnitPriceImportDto Dto { get; set; }
    public int TotalParts { get; set; }
    public int FilledParts { get; set; }
}