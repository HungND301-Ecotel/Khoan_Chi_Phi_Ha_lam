using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
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

        if (!(await CheckExistedAdjustmentFactor(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.ProcessGroupNotFound);
        }

        //Map data to Entity Model

        var adjustmentFactorCodes = dtos
            .Select(d => ExtractAdjustmentFactorCode(d.AdjustmentFactorCode))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();

        var adjusmentFactors = await _adjustmentFactorRepository.GetAllAsync(
            predicate: p => adjustmentFactorCodes.Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var adjustmentFactorIdMap = adjusmentFactors
            .Where(p => !string.IsNullOrWhiteSpace(p.Code?.Value))
            .GroupBy(p => p.Code!.Value)
            .ToDictionary(g => g.Key, g => g.First().Id);

        var excelDtos = dtos.Select(d =>
        {
            var adjustmentFactorCode = ExtractAdjustmentFactorCode(d.AdjustmentFactorCode);

            if (adjustmentFactorIdMap.TryGetValue(adjustmentFactorCode, out var adjustmentFactorId))
            {
                return AdjustmentFactorDescriptionEntity.Create(d.Id, d.Description, adjustmentFactorId, d.MaintenanceAdjustmentValue, d.ElectricityAdjustmentValue);
            }
            else
            {
                return null;
            }
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

    private async Task<bool> CheckExistedAdjustmentFactor(List<AdjustmentFactorDescriptionExcelDto> dtoList)
    {
        var dbProcessCodes = (await _adjustmentFactorRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code?.Value?.Trim())
            .Where(code => code != null)
            .ToHashSet();

        var excelProcessCodes = dtoList
            .Select(d => ExtractAdjustmentFactorCode(d.AdjustmentFactorCode))
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct();

        return excelProcessCodes.All(code => dbProcessCodes.Contains(code));
    }

    private static string ExtractAdjustmentFactorCode(string? adjustmentFactorCode)
    {
        if (string.IsNullOrWhiteSpace(adjustmentFactorCode))
        {
            return string.Empty;
        }

        var value = adjustmentFactorCode.Trim();
        var separatorIndex = value.IndexOf(" - ", StringComparison.Ordinal);

        if (separatorIndex < 0)
        {
            return value;
        }

        return value[(separatorIndex + 3)..].Trim();
    }
}