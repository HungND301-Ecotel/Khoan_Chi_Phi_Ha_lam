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

public record ImportTunnelElectricityUnitPriceEquipmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTunnelElectricityUnitPriceEquipmentExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork)
    : IRequestHandler<ImportTunnelElectricityUnitPriceEquipmentExcelCommand, bool>
{
    private readonly IWriteRepository<TunnelElectricityUnitPriceEquipment> _repository = unitOfWork.GetRepository<TunnelElectricityUnitPriceEquipment>();
    private readonly IWriteRepository<Equipment> _equipmentRepository = unitOfWork.GetRepository<Equipment>();

    public async Task<bool> Handle(ImportTunnelElectricityUnitPriceEquipmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<TunnelElectricityUnitPriceEquipmentExcelDto>(stream);

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
                var entity = TunnelElectricityUnitPriceEquipment.Create(
                    equipmentId,
                    d.MonthlyElectricityCost,
                    d.AverageMonthlyTunnelProduction,
                    startMonth,
                    endMonth);
                entity.GetType().GetProperty("Id")?.SetValue(entity, d.Id);
                return entity;
            }
            else
            {
                return TunnelElectricityUnitPriceEquipment.Create(
                    equipmentId,
                    d.MonthlyElectricityCost,
                    d.AverageMonthlyTunnelProduction,
                    startMonth,
                    endMonth);
            }
        }).ToList();

        var dbEntities = await _repository.GetAllAsync(disableTracking: false);

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
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken: cancellationToken);
            throw;
        }
    }

    private async Task<bool> CheckExistedReferences(List<TunnelElectricityUnitPriceEquipmentExcelDto> dtoList)
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
