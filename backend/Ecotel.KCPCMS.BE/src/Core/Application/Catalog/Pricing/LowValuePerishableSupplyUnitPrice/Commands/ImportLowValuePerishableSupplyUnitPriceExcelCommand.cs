using Application.Common.Caching;
using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.LowValuePerishableSupplyUnitPrice;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Pricing.LowValuePerishableSupplyUnitPrice.Commands;

public record ImportLowValuePerishableSupplyUnitPriceExcelCommand(IFormFile File, LowValuePerishableSupplyType Type) : IRequest<bool>;

public class ImportLowValuePerishableSupplyUnitPriceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<ImportLowValuePerishableSupplyUnitPriceExcelCommand, bool>
{
    private const string CacheSignalKey = "ProductUnitPrice";
    private const string ModuleCacheSignalKey = "LowValuePerishableSupplyUnitPrice";
    private readonly IWriteRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> _repository = unitOfWork.GetRepository<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice>();
    private readonly IWriteRepository<Department> _departmentRepository = unitOfWork.GetRepository<Department>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(ImportLowValuePerishableSupplyUnitPriceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException(CustomResponseMessage.FileEmpty);
        }

        List<string> importErrors = [];

        using Stream stream = request.File.OpenReadStream();
        List<LowValuePerishableSupplyUnitPriceExcelDto> dtos = excelService.ImportFromExcel<LowValuePerishableSupplyUnitPriceExcelDto>(stream) ?? [];

        var departments = await _departmentRepository.GetAllAsync(
            include: d => d.Include(x => x.Code),
            disableTracking: true);
        Dictionary<string, Department> departmentMap = departments
            .Where(d => d.Code != null)
            .ToDictionary(d => d.Code!.Value.Trim(), d => d, StringComparer.OrdinalIgnoreCase);

        ProcessGroupType processGroupType = request.Type == LowValuePerishableSupplyType.TunnelExcavation
            ? ProcessGroupType.DL
            : ProcessGroupType.LC;

        var processGroups = await _processGroupRepository.GetAllAsync(
            predicate: pg => pg.Type == processGroupType,
            include: pg => pg.Include(x => x.Code),
            disableTracking: true);
        Dictionary<string, ProcessGroup> processGroupMap = processGroups
            .Where(pg => pg.Code != null)
            .ToDictionary(pg => pg.Code!.Value.Trim(), pg => pg, StringComparer.OrdinalIgnoreCase);

        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> excelEntities = [];
        foreach (var item in dtos.Select((dto, index) => new { dto, rowNumber = index + 2 }))
        {
            try
            {
                if (!departmentMap.TryGetValue(item.dto.DepartmentCode.Trim(), out Department? department))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: đơn vị '{item.dto.DepartmentCode}' không tồn tại.");
                    continue;
                }

                if (!processGroupMap.TryGetValue(item.dto.ProcessGroupCode.Trim(), out ProcessGroup? processGroup))
                {
                    importErrors.Add($"Dòng {item.rowNumber}: nhóm công đoạn '{item.dto.ProcessGroupCode}' không hợp lệ cho loại đã chọn.");
                    continue;
                }

                DateOnly startMonth = ParseMonthYear(item.dto.StartMonth);
                DateOnly endMonth = ParseMonthYear(item.dto.EndMonth);

                Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice entity = Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice.Create(
                    department.Id,
                    processGroup.Id,
                    startMonth,
                    endMonth,
                    request.Type,
                    item.dto.TotalPrice);

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

        ValidateOverlapsWithinExcel(excelEntities, importErrors);
        ThrowIfImportErrors(importErrors);

        var dbEntities = await _repository.GetAllAsync(
            predicate: e => e.Type == request.Type,
            disableTracking: false);

        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> deleteList = [];
        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> updateList = [];
        List<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> addList = [];

        List<Guid> excelIds = excelEntities.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        deleteList.AddRange(dbEntities.Where(x => !excelIds.Contains(x.Id)));

        foreach (Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice excelEntity in excelEntities)
        {
            bool overlapExists = dbEntities.Any(e =>
                e.Id != excelEntity.Id &&
                e.DepartmentId == excelEntity.DepartmentId &&
                e.ProcessGroupId == excelEntity.ProcessGroupId &&
                e.Type == excelEntity.Type &&
                e.StartMonth <= excelEntity.EndMonth &&
                e.EndMonth >= excelEntity.StartMonth);

            if (overlapExists)
            {
                throw new ConflictException(CustomResponseMessage.LowValuePerishableSupplyUnitPriceAlreadyExists);
            }

            if (excelEntity.Id != Guid.Empty && dbEntities.Any(x => x.Id == excelEntity.Id))
            {
                Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice entityToUpdate = dbEntities.First(x => x.Id == excelEntity.Id);
                entityToUpdate.Update(
                    excelEntity.DepartmentId,
                    excelEntity.ProcessGroupId,
                    excelEntity.StartMonth,
                    excelEntity.EndMonth,
                    excelEntity.Type,
                    excelEntity.TotalPrice);
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
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateOverlapsWithinExcel(
        IReadOnlyList<Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice> excelEntities,
        ICollection<string> importErrors)
    {
        for (int index = 0; index < excelEntities.Count; index++)
        {
            Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice current = excelEntities[index];
            for (int compareIndex = index + 1; compareIndex < excelEntities.Count; compareIndex++)
            {
                Domain.Entities.Pricing.LowValuePerishableSupplyUnitPrice other = excelEntities[compareIndex];
                bool overlap = current.DepartmentId == other.DepartmentId &&
                    current.ProcessGroupId == other.ProcessGroupId &&
                    current.Type == other.Type &&
                    current.StartMonth <= other.EndMonth &&
                    current.EndMonth >= other.StartMonth;

                if (overlap)
                {
                    importErrors.Add($"Dòng {index + 2} và dòng {compareIndex + 2}: dữ liệu bị giao khoảng thời gian cho cùng đơn vị và nhóm công đoạn.");
                }
            }
        }
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