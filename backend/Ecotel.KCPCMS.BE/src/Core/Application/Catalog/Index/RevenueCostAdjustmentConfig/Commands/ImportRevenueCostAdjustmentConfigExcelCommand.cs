using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.RevenueCostAdjustmentConfig;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using RevenueCostAdjustmentConfigEntity = Domain.Entities.Index.RevenueCostAdjustmentConfig;

namespace Application.Catalog.Index.RevenueCostAdjustmentConfig.Commands;

public record ImportRevenueCostAdjustmentConfigExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportRevenueCostAdjustmentConfigExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportRevenueCostAdjustmentConfigExcelCommand, bool>
{
    private readonly IWriteRepository<RevenueCostAdjustmentConfigEntity> _revenueCostAdjustmentConfigRepository = unitOfWork.GetRepository<RevenueCostAdjustmentConfigEntity>();

    public async Task<bool> Handle(ImportRevenueCostAdjustmentConfigExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui long chon file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<RevenueCostAdjustmentConfigExcelDto>(stream);
        var dbEntities = await _revenueCostAdjustmentConfigRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<RevenueCostAdjustmentConfigEntity>();
        var updateList = new List<RevenueCostAdjustmentConfigEntity>();
        var addList = new List<RevenueCostAdjustmentConfigEntity>();

        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEntities.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in dtos)
        {
            if (dto.Id != Guid.Empty && dbEntities.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == dto.Id);
                entityToUpdate.Update(
                    dto.ProfitConditionDisplay,
                    dto.RateDisplay,
                    dto.Description);

                updateList.Add(entityToUpdate);
            }
            else
            {
                var entity = RevenueCostAdjustmentConfigEntity.Create(
                    dto.ProfitConditionDisplay,
                    dto.RateDisplay,
                    dto.Description);

                addList.Add(entity);
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _revenueCostAdjustmentConfigRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _revenueCostAdjustmentConfigRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _revenueCostAdjustmentConfigRepository.Update(updateList);
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
