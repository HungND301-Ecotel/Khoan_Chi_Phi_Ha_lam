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
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(ImportAssignmentCodeExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<AssignmentCodeExcelDto>(stream);

        //Map data to Entity Model

        var dbUnitOfMeasureNames = (await _unitOfMeasureRepository.GetAllAsync(disableTracking: true))
            .Select(p => p.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dtos.Count; i++)
        {
            var unitName = dtos[i].UnitOfMeasureName?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(unitName) && !dbUnitOfMeasureNames.Contains(unitName))
            {
                throw new BadRequestException($"Giá trị đơn vị tính '{dtos[i].UnitOfMeasureName}' không tồn tại ở dòng {i + 2}.");
            }
        }

        var unitNames = dtos
            .Select(d => d.UnitOfMeasureName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unitOfMeasure = await _unitOfMeasureRepository.GetAllAsync(
            predicate: p => unitNames.Contains(p.Name),
            disableTracking: true);
        var unitOfMeasureIdMap = unitOfMeasure.ToDictionary(p => p.Name.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var excelDtos = dtos.Select(d =>
        {
            var unitName = d.UnitOfMeasureName?.Trim();
            var unitId = !string.IsNullOrWhiteSpace(unitName) && unitOfMeasureIdMap.TryGetValue(unitName, out var id)
                ? id
                : (Guid?)null;

    return AssignmentCodeEntity.Create(d.Id, d.Name, d.Code, unitId);
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
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

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

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
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
}
