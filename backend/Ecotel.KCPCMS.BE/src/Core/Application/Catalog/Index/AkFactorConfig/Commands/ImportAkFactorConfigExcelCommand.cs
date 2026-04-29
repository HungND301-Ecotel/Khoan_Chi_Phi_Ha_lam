using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AkFactorConfig;
using Application.Interfaces.Services;
using Domain.Common.Enums;
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
            .ToDictionary(x => x.Key, x => x.First());

        var deleteList = new List<AkFactorConfigEntity>();
        var updateList = new List<AkFactorConfigEntity>();
        var addList = new List<AkFactorConfigEntity>();

        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEntities.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in dtos.Select((item, index) => new { Item = item, RowNumber = index + 2 }))
        {
            if (string.IsNullOrWhiteSpace(dto.Item.ProcessGroupCode) ||
                !processGroupByCode.TryGetValue(dto.Item.ProcessGroupCode.Trim(), out var processGroup))
            {
                throw new BadRequestException($"ProcessGroupCode khong hop le: {dto.Item.ProcessGroupCode}");
            }

            ValidateAkFactorConfig(processGroup.Type, dto.Item.AkDiffDisplay, dto.Item.AdjustmentRateDisplay, dto.RowNumber);

            if (dto.Item.Id != Guid.Empty && dbEntities.Any(x => x.Id == dto.Item.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == dto.Item.Id);
                entityToUpdate.Update(processGroup.Id, dto.Item.AkDiffDisplay, dto.Item.AdjustmentRateDisplay, dto.Item.Description);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(AkFactorConfigEntity.Create(processGroup.Id, dto.Item.AkDiffDisplay, dto.Item.AdjustmentRateDisplay, dto.Item.Description));
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

    private static void ValidateAkFactorConfig(ProcessGroupType processGroupType, string? akDiffDisplay, string? adjustmentRateDisplay, int rowNumber)
    {
        if (!AkFactorConfigEntity.SupportsProcessGroupType(processGroupType))
        {
            throw new BadRequestException($"Nhóm công đoạn sản xuất không hợp lệ để cấu hình hệ số Ak ở dòng {rowNumber}.");
        }

        if (!AkFactorConfigEntity.HasValidAkDiffCondition(akDiffDisplay))
        {
            throw new BadRequestException($"Chênh lệch Ak không đúng định dạng ở dòng {rowNumber}. Vui lòng dùng dạng > 0, <= -0,5 hoặc = 1.");
        }

        if (!AkFactorConfigEntity.HasValidAdjustmentRate(adjustmentRateDisplay))
        {
            throw new BadRequestException($"Tỷ lệ điều chỉnh doanh thu không đúng định dạng ở dòng {rowNumber}. Vui lòng dùng một giá trị duy nhất, ví dụ 1,5%.");
        }
    }
}
