using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Shared.Constants;
using Microsoft.EntityFrameworkCore;
using MechanizedTransportOverheadUnitPriceEntity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportOverheadUnitPrice;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.MechanizedTransportOverheadUnitPrice.Commands;

public record ImportMechanizedTransportOverheadUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportMechanizedTransportOverheadUnitPriceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportMechanizedTransportOverheadUnitPriceExcelCommand, bool>
{
    private readonly IWriteRepository<MechanizedTransportOverheadUnitPriceEntity> _repository = unitOfWork.GetRepository<MechanizedTransportOverheadUnitPriceEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(ImportMechanizedTransportOverheadUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        List<string> importErrors = [];

        using Stream stream = request.File.OpenReadStream();
        List<MechanizedTransportOverheadUnitPriceExcelDto> dtos = excelService.ImportFromExcel<MechanizedTransportOverheadUnitPriceExcelDto>(stream) ?? [];

        var processGroups = await _processGroupRepository.GetAllAsync(
            include: query => query.Include(p => p.Code),
            disableTracking: true);
        Dictionary<string, ProcessGroup> processGroupMap = processGroups
            .Where(p => p.Code != null)
            .ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var dbEntities = await _repository.GetAllAsync(disableTracking: false);

        List<MechanizedTransportOverheadUnitPriceEntity> excelEntities = [];
        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                string? processGroupCode = ExtractCode(item.dto.ProcessGroupCode);
                if (processGroupCode == null ||
                    !processGroupMap.TryGetValue(processGroupCode, out ProcessGroup? processGroup))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: nhóm công đoạn sản xuất '{item.dto.ProcessGroupCode}' không tồn tại.");
                    continue;
                }

                DateOnly startMonth = ParseMonthYear(item.dto.StartMonth);
                DateOnly endMonth = ParseMonthYear(item.dto.EndMonth);

                bool isDuplicated = dbEntities.Any(x =>
                        x.ProcessGroupId == processGroup.Id
                        && x.StartMonth == startMonth
                        && x.EndMonth == endMonth
                        && x.Id != item.dto.Id)
                    || excelEntities.Any(x =>
                        x.ProcessGroupId == processGroup.Id
                        && x.StartMonth == startMonth
                        && x.EndMonth == endMonth);

                if (isDuplicated)
                {
                    importErrors.Add($"Dòng {item.rowNumber}: đã tồn tại đơn giá cho nhóm công đoạn '{item.dto.ProcessGroupCode}' trong khoảng thời gian này.");
                    continue;
                }

                var entity = MechanizedTransportOverheadUnitPriceEntity.Create(
                    processGroup.Id,
                    startMonth,
                    endMonth,
                    item.dto.LowValuePerishableSupplyUnitPrice,
                    item.dto.ElectricityUnitPrice);

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

        List<MechanizedTransportOverheadUnitPriceEntity> deleteList = [];
        List<MechanizedTransportOverheadUnitPriceEntity> updateList = [];
        List<MechanizedTransportOverheadUnitPriceEntity> addList = [];

        List<Guid> excelIds = excelEntities.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        deleteList.AddRange(dbEntities.Where(x => !excelIds.Contains(x.Id)));

        foreach (var excelEntity in excelEntities)
        {
            if (excelEntity.Id != Guid.Empty && dbEntities.Any(x => x.Id == excelEntity.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == excelEntity.Id);
                entityToUpdate.Update(
                    excelEntity.ProcessGroupId,
                    excelEntity.StartMonth,
                    excelEntity.EndMonth,
                    excelEntity.LowValuePerishableSupplyUnitPrice,
                    excelEntity.ElectricityUnitPrice);
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
            if (deleteList.Count > 0)
            {
                _repository.Delete(deleteList);
            }

            if (addList.Count > 0)
            {
                await _repository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Count > 0)
            {
                _repository.Update(updateList);
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

    private static string? ExtractCode(string? combinedValue)
    {
        if (string.IsNullOrWhiteSpace(combinedValue))
        {
            return null;
        }

        int separatorIndex = combinedValue.IndexOf(" - ", StringComparison.Ordinal);
        string code = separatorIndex >= 0 ? combinedValue[..separatorIndex] : combinedValue;
        return code.Trim();
    }

    private static DateOnly ParseMonthYear(string monthYear)
    {
        if (string.IsNullOrWhiteSpace(monthYear))
        {
            return DateOnly.MinValue;
        }

        if (DateOnly.TryParseExact(monthYear, "MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateOnly result))
        {
            return result;
        }

        if (DateOnly.TryParseExact(monthYear, "M/yyyy", null, System.Globalization.DateTimeStyles.None, out result))
        {
            return result;
        }

        if (DateTime.TryParse(monthYear, out DateTime dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        throw new BadRequestException($"Không thể parse tháng năm: {monthYear}. Định dạng cần là MM/yyyy hoặc M/yyyy");
    }

    private static void ThrowIfImportErrors(List<string> importErrors)
    {
        List<string> errors = importErrors
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
