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

public record ImportPowerExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportPowerExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportPowerExcelCommand, bool>
{
    private readonly IWriteRepository<Power> _PowerRepository = unitOfWork.GetRepository<Power>();
    public async Task<bool> Handle(ImportPowerExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<PowerExcelDto>(stream)).Adapt<List<Power>>();
        var dbPower = await _PowerRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<Power>();
        var updateList = new List<Power>();
        var addList = new List<Power>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbPower.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbPower.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbPower.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(Power.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _PowerRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _PowerRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _PowerRepository.Update(updateList);
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