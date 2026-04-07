using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.SavingsRateConfig;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using SavingsRateConfigEntity = Domain.Entities.Index.SavingsRateConfig;

namespace Application.Catalog.Index.SavingsRateConfig.Commands;

public record ImportSavingsRateConfigExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportSavingsRateConfigExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportSavingsRateConfigExcelCommand, bool>
{
    private readonly IWriteRepository<SavingsRateConfigEntity> _savingsRateConfigRepository = unitOfWork.GetRepository<SavingsRateConfigEntity>();

    public async Task<bool> Handle(ImportSavingsRateConfigExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui long chon file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<SavingsRateConfigExcelDto>(stream);
        var dbEntities = await _savingsRateConfigRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<SavingsRateConfigEntity>();
        var updateList = new List<SavingsRateConfigEntity>();
        var addList = new List<SavingsRateConfigEntity>();

        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbEntities.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in dtos)
        {
            if (dto.Id != Guid.Empty && dbEntities.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbEntities.First(x => x.Id == dto.Id);
                entityToUpdate.Update(dto.MaxRevenue, dto.MaxSavingsRate, dto.Description);
                updateList.Add(entityToUpdate);
            }
            else
            {
                addList.Add(SavingsRateConfigEntity.Create(dto.MaxRevenue, dto.MaxSavingsRate, dto.Description));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _savingsRateConfigRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _savingsRateConfigRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _savingsRateConfigRepository.Update(updateList);
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
