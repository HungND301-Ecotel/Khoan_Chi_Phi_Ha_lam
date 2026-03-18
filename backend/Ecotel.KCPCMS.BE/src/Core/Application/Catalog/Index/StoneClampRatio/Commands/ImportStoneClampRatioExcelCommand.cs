using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using StoneClampRatioEntity = Domain.Entities.Index.StoneClampRatio;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record ImportStoneClampRatioExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportStoneClampRatioExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportStoneClampRatioExcelCommand, bool>
{
    private readonly IWriteRepository<StoneClampRatioEntity> _stoneClampRatioRepository = unitOfWork.GetRepository<StoneClampRatioEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();
    private readonly IWriteRepository<Hardness> _hardnessRepository = unitOfWork.GetRepository<Hardness>();
    public async Task<bool> Handle(ImportStoneClampRatioExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<StoneClampRatioExcelDto>(stream);

        if (!(await CheckExistedProductionProcess(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.ProductionProcessNotFound);
        }

        if (!(await CheckExistedHardness(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.HardnessNotFound);
        }

        //Map data to Entity Model
        var productionProcess = await _productProcessRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.ProcessCode).Contains(p.Code.Value),
            include: p => p.Include(p => p.Code),
            disableTracking: true);
        var processIdMap = productionProcess.ToDictionary(p => p.Code.Value, p => p.Id);

        var hardness = await _hardnessRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.HardnessValue).Contains(p.Value),
            disableTracking: true);
        var hardnessIdMap = hardness.ToDictionary(p => p.Value, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (hardnessIdMap.TryGetValue(d.HardnessValue, out var hardnessId) && processIdMap.TryGetValue(d.ProcessCode, out var processId))
            {
                return StoneClampRatioEntity.Create(d.Id, d.Value, d.CoefficientValue, hardnessId, processId);
            }
            else
            {
                return null;
            }
        }).Where(d => d != null).ToList();


        var dbStoneClampRatio = await _stoneClampRatioRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<StoneClampRatioEntity>();
        var updateList = new List<StoneClampRatioEntity>();
        var addList = new List<StoneClampRatioEntity>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbStoneClampRatio.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbStoneClampRatio.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbStoneClampRatio.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value, dto.CoefficientValue, dto.HardnessId, dto.ProcessId);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(StoneClampRatioEntity.Create(dto.Value, dto.CoefficientValue, dto.HardnessId, dto.ProcessId));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _stoneClampRatioRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _stoneClampRatioRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _stoneClampRatioRepository.Update(updateList);
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

    private async Task<bool> CheckExistedProductionProcess(List<StoneClampRatioExcelDto> dtoList)
    {
        var dbProcessCodes = (await _productProcessRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code?.Value?.Trim())
            .Where(code => code != null)
            .ToHashSet();

        var excelProcessCodes = dtoList
            .Select(d => d.ProcessCode?.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct();

        return excelProcessCodes.All(code => dbProcessCodes.Contains(code));
    }

    private async Task<bool> CheckExistedHardness(List<StoneClampRatioExcelDto> dtoList)
    {
        var dbHardnessValues = (await _hardnessRepository.GetAllAsync(disableTracking: true))
            .Select(p => p.Value.Trim())
            .ToHashSet();

        var excelHardnessValues = dtoList
            .Select(d => d.HardnessValue?.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct();

        return excelHardnessValues.All(v => dbHardnessValues.Contains(v));
    }
}