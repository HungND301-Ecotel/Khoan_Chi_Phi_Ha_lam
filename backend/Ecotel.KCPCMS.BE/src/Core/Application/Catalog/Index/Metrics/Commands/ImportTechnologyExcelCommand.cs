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

public record ImportTechnologyExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTechnologyExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportTechnologyExcelCommand, bool>
{
    private readonly IWriteRepository<Technology> _technologyRepository = unitOfWork.GetRepository<Technology>();
    public async Task<bool> Handle(ImportTechnologyExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<TechnologyExcelDto>(stream)).Adapt<List<Technology>>();
        var dbTechnology = await _technologyRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<Technology>();
        var updateList = new List<Technology>();
        var addList = new List<Technology>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbTechnology.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbTechnology.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbTechnology.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(Technology.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _technologyRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _technologyRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _technologyRepository.Update(updateList);
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
