using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.MechanizedTransportUnitPrices;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing.MechanizedTransportUnitPrice;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.MechanizedTransportUnitPrices.ExcavatorAndBulldozerUnitPrice.Commands;

public record ImportExcavatorAndBulldozerUnitPriceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportExcavatorAndBulldozerUnitPriceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportExcavatorAndBulldozerUnitPriceExcelCommand, bool>
{
    private static readonly string[] ValidEquipmentQualities = { "A", "B", "C" };

    private readonly IWriteRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice>();
    private readonly IWriteRepository<AssignmentCode> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCode>();
    private readonly IWriteRepository<ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<ProductionProcess>();

    public async Task<bool> Handle(ImportExcavatorAndBulldozerUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        List<string> importErrors = [];

        using Stream stream = request.File.OpenReadStream();
        List<ExcavatorAndBulldozerUnitPriceExcelDto> dtos = excelService.ImportFromExcel<ExcavatorAndBulldozerUnitPriceExcelDto>(stream) ?? [];

        var assignmentCodes = await _assignmentCodeRepository.GetAllAsync(
            include: query => query.Include(a => a.Code),
            disableTracking: true);
        Dictionary<string, AssignmentCode> assignmentCodeMap = assignmentCodes
            .Where(a => a.Code != null)
            .ToDictionary(a => a.Code!.Value.Trim(), a => a, StringComparer.OrdinalIgnoreCase);

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            predicate: p => p.ProcessGroup != null && p.ProcessGroup.Type == ProcessGroupType.VTCG,
            include: query => query.Include(p => p.Code),
            disableTracking: true);
        Dictionary<string, ProductionProcess> productionProcessMap = productionProcesses
            .Where(p => p.Code != null)
            .ToDictionary(p => p.Code!.Value.Trim(), p => p, StringComparer.OrdinalIgnoreCase);

        var dbEntities = await _repository.GetAllAsync(
            include: query => query.Include(e => e.Details),
            disableTracking: false);

        List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> excelEntities = [];

        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                string? assignmentCode = ExtractCode(item.dto.AssignmentCodeCode);
                if (assignmentCode == null || !assignmentCodeMap.TryGetValue(assignmentCode, out AssignmentCode? assignment))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: nhóm vật tư, tài sản '{item.dto.AssignmentCodeCode}' không tồn tại.");
                    continue;
                }

                string? productionProcessCode = ExtractCode(item.dto.ProductionProcessCode);
                if (productionProcessCode == null || !productionProcessMap.TryGetValue(productionProcessCode, out ProductionProcess? productionProcess))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: công đoạn sản xuất '{item.dto.ProductionProcessCode}' không tồn tại hoặc không thuộc Vận tải cơ giới.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.dto.EquipmentQuality) || !ValidEquipmentQualities.Contains(item.dto.EquipmentQuality.Trim().ToUpper()))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: chất lượng thiết bị phải là A, B hoặc C.");
                    continue;
                }

                DateOnly startMonth = ParseMonthYear(item.dto.StartMonth);
                DateOnly endMonth = ParseMonthYear(item.dto.EndMonth);
                string quality = item.dto.EquipmentQuality.Trim().ToUpper();

                bool isDuplicated = dbEntities.Any(x =>
                        x.AssignmentCodeId == assignment.Id
                        && x.EquipmentQuality == quality
                        && x.ProductionProcessId == productionProcess.Id
                        && x.StartMonth == startMonth
                        && x.EndMonth == endMonth
                        && x.Id != item.dto.Id)
                    || excelEntities.Any(x =>
                        x.AssignmentCodeId == assignment.Id
                        && x.EquipmentQuality == quality
                        && x.ProductionProcessId == productionProcess.Id
                        && x.StartMonth == startMonth
                        && x.EndMonth == endMonth);

                if (isDuplicated)
                {
                    importErrors.Add($"Dòng {item.rowNumber}: đã tồn tại đơn giá cho thiết bị/công đoạn/khoảng thời gian này.");
                    continue;
                }

                var details = new List<MechanizedTransportUnitPriceDetailInput>
                {
                    new(null, item.dto.FuelUnitPrice, null, item.dto.MaintenanceUnitPrice)
                };

                var entity = Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice.Create(
                    assignment.Id,
                    quality,
                    productionProcess.Id,
                    startMonth,
                    endMonth,
                    details);

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

        List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> deleteList = [];
        List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> updateList = [];
        List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.ExcavatorAndBulldozerUnitPrice> addList = [];

        List<Guid> excelIds = excelEntities.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        deleteList.AddRange(dbEntities.Where(x => !excelIds.Contains(x.Id)));

        foreach (var excelEntity in excelEntities)
        {
            if (excelEntity.Id != Guid.Empty && dbEntities.Any(x => x.Id == excelEntity.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == excelEntity.Id);
                var excelDetail = excelEntity.Details.First();
                var details = new List<Domain.Entities.Pricing.MechanizedTransportUnitPrice.MechanizedTransportUnitPriceDetailInput>
                {
                    new(null, excelDetail.FuelUnitPrice, null, excelDetail.MaintenanceUnitPrice)
                };

                entityToUpdate.Update(
                    excelEntity.AssignmentCodeId,
                    excelEntity.EquipmentQuality,
                    excelEntity.ProductionProcessId,
                    excelEntity.StartMonth,
                    excelEntity.EndMonth,
                    details);
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