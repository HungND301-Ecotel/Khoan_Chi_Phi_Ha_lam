using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactor;
using Application.Interfaces.Services;
using Domain.Common.Enums;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using AdjustmentFactorEntity = Domain.Entities.Index.AdjustmentFactor;

namespace Application.Catalog.Index.AdjustmentFactor.Commands;

public record ImportAdjustmentFactorExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAdjustmentFactorExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<ImportAdjustmentFactorExcelCommand, bool>
{
    private readonly IWriteRepository<AdjustmentFactorEntity> _adjustmentFactorRepository = unitOfWork.GetRepository<AdjustmentFactorEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<bool> Handle(ImportAdjustmentFactorExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AdjustmentFactorExcelDto>(stream);

        if (!(await CheckExistedProductionProcess(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.ProcessGroupNotFound);
        }

        //Map data to Entity Model

        var processGroup = await _processGroupRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.ProcessGroupCode).Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var processGroupIdMap = processGroup.ToDictionary(p => p.Code.Value, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (processGroupIdMap.TryGetValue(d.ProcessGroupCode, out var processGroupId))
            {
                return AdjustmentFactorEntity.Create(d.Id, (AdjustmentFactorType)d.Type, d.Code, d.Name, processGroupId);
            }
            else
            {
                return null;
            }
        }).Where(d => d != null).ToList();


        var dbAdjustmentFactor = await _adjustmentFactorRepository.GetAllAsync(
            include: a => a.Include(a => a.Code),
            disableTracking: true);

        var deleteList = new List<AdjustmentFactorEntity>();
        var updateList = new List<AdjustmentFactorEntity>();
        var addList = new List<AdjustmentFactorEntity>();

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
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code?.Value ?? "", dto.Type, dto.Name, dto.ProcessGroupId);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                }
                addList.Add(AdjustmentFactorEntity.Create(dto.Code?.Value ?? "", dto.Type, dto.Name, dto.ProcessGroupId));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _adjustmentFactorRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _adjustmentFactorRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _adjustmentFactorRepository.Update(updateList);
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

    private async Task<bool> CheckExistedProductionProcess(List<AdjustmentFactorExcelDto> dtoList)
    {
        var dbProcessCodes = (await _processGroupRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code?.Value?.Trim())
            .Where(code => code != null)
            .ToHashSet();

        var excelProcessCodes = dtoList
            .Select(d => d.ProcessGroupCode?.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct();

        return excelProcessCodes.All(code => dbProcessCodes.Contains(code));
    }
}