using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.UnitOfMeasure;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Index.UnitOfMeasures.Commands;

public record ImportUnitOfMeasureExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportUnitOfMeasureExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportUnitOfMeasureExcelCommand, bool>
{
    private readonly IWriteRepository<UnitOfMeasure> _unitOfMeasureRepository = unitOfWork.GetRepository<UnitOfMeasure>();
    public async Task<bool> Handle(ImportUnitOfMeasureExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<UnitOfMeasureExcelDto>(stream)).Adapt<List<UnitOfMeasure>>();
        var dbUnitOfMeasures = await _unitOfMeasureRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<UnitOfMeasure>();
        var updateList = new List<UnitOfMeasure>();
        var addList = new List<UnitOfMeasure>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbUnitOfMeasures.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbUnitOfMeasures.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbUnitOfMeasures.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Name);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(UnitOfMeasure.Create(dto.Name));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _unitOfMeasureRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _unitOfMeasureRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _unitOfMeasureRepository.Update(updateList);
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