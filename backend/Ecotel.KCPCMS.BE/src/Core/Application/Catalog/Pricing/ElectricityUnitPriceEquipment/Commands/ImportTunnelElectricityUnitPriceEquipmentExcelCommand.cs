using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record ImportTunnelElectricityUnitPriceEquipmentExcelCommand(
    IFormFile File,
    ElectricityUnitPriceType Type = ElectricityUnitPriceType.TunnelExcavation) : IRequest<bool>;

public class ImportTunnelElectricityUnitPriceEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICacheService cacheService)
    : IRequestHandler<ImportTunnelElectricityUnitPriceEquipmentExcelCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "ElectricityUnitPriceEquipment";
    private readonly IWriteRepository<TunnelElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<TunnelElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<bool> Handle(ImportTunnelElectricityUnitPriceEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        var importErrors = new List<string>();

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<TunnelElectricityUnitPriceEquipmentExcelDto>(stream) ?? [];

        await CollectReferenceErrors(dtos, importErrors);

        // Map data to Entity Model
        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e.Include(e => e.Code),
            disableTracking: true);
        var equipmentIdMap = equipments.Where(e => e.Code != null).ToDictionary(e => e.Code!.Value, e => e.Id);

        var excelEntities = new List<TunnelElectricityUnitPriceEquipment>();
        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                if (!equipmentIdMap.TryGetValue(item.dto.EquipmentCode, out var equipmentId))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: thiết bị '{item.dto.EquipmentCode}' không tồn tại.");
                    continue;
                }

                var startMonth = ParseMonthYear(item.dto.StartMonth);
                var endMonth = ParseMonthYear(item.dto.EndMonth);

                TunnelElectricityUnitPriceEquipment entity = request.Type == ElectricityUnitPriceType.Trimming
                    ? TrimmingElectricityUnitPriceEquipment.Create(
                        equipmentId,
                        item.dto.MonthlyElectricityCost,
                        item.dto.AverageMonthlyTunnelProduction,
                        startMonth,
                        endMonth)
                    : TunnelElectricityUnitPriceEquipment.Create(
                        equipmentId,
                        item.dto.MonthlyElectricityCost,
                        item.dto.AverageMonthlyTunnelProduction,
                        startMonth,
                        endMonth,
                        request.Type);

                if (item.dto.Id != Guid.Empty)
                {
                    entity.GetType().GetProperty("Id")?.SetValue(entity, item.dto.Id);
                }

                excelEntities.Add(entity);
            }
            catch (Exception ex) when (ex is BadRequestException or ArgumentException)
            {
                importErrors.Add($"Dòng {item.rowNumber}: {ex.Message}");
            }
        }

        ThrowIfImportErrors(importErrors);

        var dbEntities = await _repository.GetAllAsync(
            predicate: e => e.ElectricityType == request.Type,
            disableTracking: false);

        var deleteList = new List<TunnelElectricityUnitPriceEquipment>();
        var updateList = new List<TunnelElectricityUnitPriceEquipment>();
        var addList = new List<TunnelElectricityUnitPriceEquipment>();

        // CheckDelete
        var excelIds = excelEntities.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEntities.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var excelEntity in excelEntities)
        {
            if (excelEntity.Id != Guid.Empty && dbEntities.Any(x => x.Id == excelEntity.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == excelEntity.Id);
                entityToUpdate.Update(
                    excelEntity.EquipmentId,
                    excelEntity.MonthlyElectricityCost,
                    excelEntity.AverageMonthlyTunnelProduction,
                    excelEntity.StartMonth,
                    excelEntity.EndMonth);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(excelEntity);
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

    private async Task CollectReferenceErrors(List<TunnelElectricityUnitPriceEquipmentExcelDto> dtoList, ICollection<string> importErrors)
    {
        var dbEquipmentCodes = (await _equipmentRepository.GetAllAsync(
                include: e => e.Include(e => e.Code),
                disableTracking: true))
            .Where(e => e.Code != null)
            .Select(e => e.Code!.Value.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var item in dtoList.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            var equipmentCode = item.dto.EquipmentCode?.Trim();
            if (!string.IsNullOrWhiteSpace(equipmentCode) && !dbEquipmentCodes.Contains(equipmentCode))
            {
                importErrors.Add($"Dòng {item.rowNumber}: thiết bị '{equipmentCode}' không tồn tại.");
            }
        }
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
}
