using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AdjustmentFactorDescription;
using Application.Dto.Catalog.AssignmentCode;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using AssignmentCodeEntity = Domain.Entities.Index.AssignmentCode;

namespace Application.Catalog.Index.AssignmentCodes.Commands;

public record ImportAssignmentCodeExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAssignmentCodeExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportAssignmentCodeExcelCommand, bool>
{
    private readonly IWriteRepository<AssignmentCodeEntity> _assignmentCodeRepository = unitOfWork.GetRepository<AssignmentCodeEntity>();
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(ImportAssignmentCodeExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AssignmentCodeExcelDto>(stream);

        if (!(await CheckExistedUnitOfMeasure(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.ProcessGroupNotFound);
        }

        //Map data to Entity Model

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.UnitOfMeasureName).Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (unitOfMeasureIdMap.TryGetValue(d.UnitOfMeasureName, out var unitOfMeasureId))
            {
                return AssignmentCodeEntity.Create(d.Id, d.Name, d.Code, unitOfMeasureId);
            }
            else
            {
                return AssignmentCodeEntity.Create(d.Id, d.Name, d.Code, null);
            }
        }).ToList();


        var dbAdjustmentFactor = await _assignmentCodeRepository.GetAllAsync(
            include: a => a.Include(a => a.Code!),
            disableTracking: true);

        var deleteList = new List<AssignmentCodeEntity>();
        var updateList = new List<AssignmentCodeEntity>();
        var addList = new List<AssignmentCodeEntity>();

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
                    entityToUpdate.Update(dto.Name, dto.Code.Value, dto.UnitOfMeasureId);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(AssignmentCodeEntity.Create(dto.Name, dto.Code.Value, dto.UnitOfMeasureId));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _assignmentCodeRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _assignmentCodeRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _assignmentCodeRepository.Update(updateList);
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

    private async Task<bool> CheckExistedUnitOfMeasure(List<AssignmentCodeExcelDto> dtoList)
    {
        var dbProcessNames = (await _unitOfMeasureRepository.GetAllAsync(
                disableTracking: true))
            .Select(p => p.Name.Trim())
            .Where(n => n != null)
            .ToHashSet();

        var excelAssingmentCodes = dtoList
            .Select(d => d.UnitOfMeasureName?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct();

        return excelAssingmentCodes.All(name => dbProcessNames.Contains(name));
    }
}