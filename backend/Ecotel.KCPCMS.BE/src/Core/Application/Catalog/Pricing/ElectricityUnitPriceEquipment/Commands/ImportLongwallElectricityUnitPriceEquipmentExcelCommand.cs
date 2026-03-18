using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ElectricityUnitPriceEquipment;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Domain.Entities.Pricing.EletricityUnitPrice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Pricing.ElectricityUnitPriceEquipment.Commands;

public record ImportLongwallElectricityUnitPriceEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportLongwallElectricityUnitPriceEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportLongwallElectricityUnitPriceEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<LongwallElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<LongwallElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<bool> Handle(ImportLongwallElectricityUnitPriceEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<LongwallElectricityUnitPriceEquipmentExcelDto>(stream);

        if (!(await CheckExistedReferences(dtos)))
        {
            throw new BadRequestException("Tồn tại dữ liệu tham chiếu không hợp lệ.");
        }

        // Map data to Entity Model
        var equipments = await _equipmentRepository.GetAllAsync(
            include: e => e.Include(e => e.Code),
            disableTracking: true);
        var equipmentIdMap = equipments.Where(e => e.Code != null).ToDictionary(e => e.Code!.Value, e => e.Id);

        var excelEntities = dtos.Select(d =>
        {
            equipmentIdMap.TryGetValue(d.EquipmentCode, out var equipmentId);
            var startMonth = ParseMonthYear(d.StartMonth);
            var endMonth = ParseMonthYear(d.EndMonth);

            if (d.Id != Guid.Empty)
            {
                var entity = LongwallElectricityUnitPriceEquipment.Create(
                    equipmentId,
                    startMonth,
                    endMonth,
                    d.Quantity,
                    d.Pdm,
                    d.Kyc,
                    d.Kdt,
                    d.WorkingHour,
                    d.WorkingDate,
                    d.AverageMonthlyTunnelProduction);
                entity.GetType().GetProperty("Id")?.SetValue(entity, d.Id);
                return entity;
            }
            else
            {
                return LongwallElectricityUnitPriceEquipment.Create(
                    equipmentId,
                    startMonth,
                    endMonth,
                    d.Quantity,
                    d.Pdm,
                    d.Kyc,
                    d.Kdt,
                    d.WorkingHour,
                    d.WorkingDate,
                    d.AverageMonthlyTunnelProduction);
            }
        }).ToList();

        var dbEntities = await _repository.GetAllAsync(disableTracking: false);

        var deleteList = new List<LongwallElectricityUnitPriceEquipment>();
        var updateList = new List<LongwallElectricityUnitPriceEquipment>();
        var addList = new List<LongwallElectricityUnitPriceEquipment>();

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
                    excelEntity.StartMonth,
                    excelEntity.EndMonth,
                    excelEntity.Quantity,
                    excelEntity.Pdm,
                    excelEntity.Kyc,
                    excelEntity.Kdt,
                    excelEntity.WorkingHour,
                    excelEntity.WorkingDate,
                    excelEntity.AverageMonthlyTunnelProduction);
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
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private async Task<bool> CheckExistedReferences(List<LongwallElectricityUnitPriceEquipmentExcelDto> dtoList)
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
