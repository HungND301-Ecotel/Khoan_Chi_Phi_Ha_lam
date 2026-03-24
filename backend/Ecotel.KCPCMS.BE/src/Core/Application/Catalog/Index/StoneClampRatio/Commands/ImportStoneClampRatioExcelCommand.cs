using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.StoneClampRatio;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using StoneClampRatioEntity = Domain.Entities.Index.StoneClampRatio;

namespace Application.Catalog.Index.StoneClampRatio.Commands;

public record ImportStoneClampRatioExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportStoneClampRatioExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportStoneClampRatioExcelCommand, bool>
{
    private readonly IWriteRepository<StoneClampRatioEntity> _stoneClampRatioRepository = unitOfWork.GetRepository<StoneClampRatioEntity>();
    public async Task<bool> Handle(ImportStoneClampRatioExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<StoneClampRatioExcelDto>(stream);

        var excelDtos = dtos.Select(d => StoneClampRatioEntity.Create(d.Id, d.Value)).ToList();


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
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(StoneClampRatioEntity.Create(dto.Value));
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
}