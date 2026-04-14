using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Domain.Entities.Index;
using AkFactorConfigEntity = Domain.Entities.Index.AkFactorConfig;

namespace Application.Catalog.Index.AkFactorConfig.Commands;

public record ImportAkFactorConfigExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportAkFactorConfigExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportAkFactorConfigExcelCommand, bool>
{
    private readonly IWriteRepository<AkFactorConfigEntity> _akFactorConfigRepository = unitOfWork.GetRepository<AkFactorConfigEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();

    public async Task<bool> Handle(ImportAkFactorConfigExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui long chon file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<AkFactorConfigExcelDto>(stream);
        var dbEntities = await _akFactorConfigRepository.GetAllAsync(disableTracking: true);
        var processGroups = await _processGroupRepository.GetAllAsync(disableTracking: true);
        var processGroupByCode = processGroups
            .Where(x => x.Code != null && !string.IsNullOrWhiteSpace(x.Code.Value))
            .GroupBy(x => x.Code!.Value)
            .ToDictionary(x => x.Key, x => x.First().Id);

        var deleteList = new List<AkFactorConfigEntity>();
        var updateList = new List<AkFactorConfigEntity>();
        var addList = new List<AkFactorConfigEntity>();

        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEntities.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in dtos)
        {
            if (string.IsNullOrWhiteSpace(dto.ProcessGroupCode) ||
                !processGroupByCode.TryGetValue(dto.ProcessGroupCode.Trim(), out var processGroupId))
            {
                throw new BadRequestException($"ProcessGroupCode khong hop le: {dto.ProcessGroupCode}");
            }

            if (dto.Id != Guid.Empty && dbEntities.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == dto.Id);
                entityToUpdate.Update(processGroupId, dto.AkDiffDisplay, dto.AdjustmentRateDisplay, dto.Description);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(AkFactorConfigEntity.Create(processGroupId, dto.AkDiffDisplay, dto.AdjustmentRateDisplay, dto.Description));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _akFactorConfigRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _akFactorConfigRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _akFactorConfigRepository.Update(updateList);
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
}
