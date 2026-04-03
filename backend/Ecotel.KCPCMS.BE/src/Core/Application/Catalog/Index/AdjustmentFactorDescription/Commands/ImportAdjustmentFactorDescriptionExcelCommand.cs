using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using AdjustmentFactorDescriptionEntity = Domain.Entities.Index.AdjustmentFactorDescription;
using AdjustmentFactorEntity = Domain.Entities.Index.AdjustmentFactor;

namespace Application.Catalog.Index.AdjustmentFactorDescription.Commands;

public record ImportAdjustmentFactorDescriptionExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAdjustmentFactorDescriptionExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportAdjustmentFactorDescriptionExcelCommand, bool>
{
    private readonly IWriteRepository<AdjustmentFactorDescriptionEntity> _adjustmentFactorDescriptionRepository = unitOfWork.GetRepository<AdjustmentFactorDescriptionEntity>();
    private readonly IWriteRepository<AdjustmentFactorEntity> _adjustmentFactorRepository = unitOfWork.GetRepository<AdjustmentFactorEntity>();
    public async Task<bool> Handle(ImportAdjustmentFactorDescriptionExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AdjustmentFactorDescriptionExcelDto>(stream);

        //Map data to Entity Model

        //var dbAdjustmentFactorCodes = (await _adjustmentFactorRepository.GetAllAsync(
        //        include: p => p.Include(p => p.Code).Include(p => p.ProcessGroup).ThenInclude(pg => pg.Code),
        //        disableTracking: true))
        //    .Select(p => p.Code?.Value?.Trim())
        //    .Where(code => !string.IsNullOrWhiteSpace(code))
        //    .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dbAdjustmentFactorMap = (await _adjustmentFactorRepository.GetAllAsync(
                include: p => p.Include(p => p.Code).Include(p => p.ProcessGroup).ThenInclude(pg => pg.Code),
                disableTracking: true))
            .Where(p => !string.IsNullOrWhiteSpace(p.Code?.Value))
            .GroupBy(p => $"{p.ProcessGroup?.Code?.Value?.Trim()}|{p.Code!.Value.Trim()}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dtos.Count; i++)
        {
            var (pgCode, afCode) = ExtractCodes(dtos[i].AdjustmentFactorCode);
            var compositeKey = $"{pgCode}|{afCode}";
            if (string.IsNullOrWhiteSpace(afCode) || !dbAdjustmentFactorMap.ContainsKey(compositeKey))
            {
                throw new BadRequestException($"Giá trị mã hệ số điều chỉnh '{dtos[i].AdjustmentFactorCode}' không tồn tại ở dòng {i + 2}.");
            }
        }

        var excelDtos = dtos.Select(d =>
        {
            var (pgCode, afCode) = ExtractCodes(d.AdjustmentFactorCode);
            var compositeKey = $"{pgCode}|{afCode}";

            if (dbAdjustmentFactorMap.TryGetValue(compositeKey, out var adjustmentFactorId))
            {
                return AdjustmentFactorDescriptionEntity.Create(d.Id, d.Description, adjustmentFactorId, d.MaintenanceAdjustmentValue, d.ElectricityAdjustmentValue);
            }
            return null;
        }).Where(d => d != null).ToList();


        var dbAdjustmentFactor = await _adjustmentFactorDescriptionRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<AdjustmentFactorDescriptionEntity>();
        var updateList = new List<AdjustmentFactorDescriptionEntity>();
        var addList = new List<AdjustmentFactorDescriptionEntity>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbAdjustmentFactor.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbAdjustmentFactor.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbAdjustmentFactor.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Description, dto.AdjustmentFactorId, dto.MaintenanceAdjustmentValue, dto.ElectricityAdjustmentValue);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(AdjustmentFactorDescriptionEntity.Create(dto.Description, dto.AdjustmentFactorId, dto.MaintenanceAdjustmentValue, dto.ElectricityAdjustmentValue));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _adjustmentFactorDescriptionRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _adjustmentFactorDescriptionRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _adjustmentFactorDescriptionRepository.Update(updateList);
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

    private static (string ProcessGroupCode, string AdjustmentFactorCode) ExtractCodes(string? adjustmentFactorCode)
    {
        if (string.IsNullOrWhiteSpace(adjustmentFactorCode))
        {
            return (string.Empty, string.Empty);
        }

        var value = adjustmentFactorCode.Trim();
        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            return (string.Empty, value);
        }

        return (value[..separatorIndex].Trim(), value[(separatorIndex + 3)..].Trim());
    }
}
