using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Index.Metrics.Commands;

public record ImportInsertItemExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportInsertItemExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportInsertItemExcelCommand, bool>
{
    private readonly IWriteRepository<InsertItem> _insertItemRepository = unitOfWork.GetRepository<InsertItem>();
    public async Task<bool> Handle(ImportInsertItemExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<InsertItemExcelDto>(stream)).Adapt<List<InsertItem>>();
        var dbInsertItem = await _insertItemRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<InsertItem>();
        var updateList = new List<InsertItem>();
        var addList = new List<InsertItem>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbInsertItem.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbInsertItem.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbInsertItem.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(InsertItem.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _insertItemRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _insertItemRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _insertItemRepository.Update(updateList);
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