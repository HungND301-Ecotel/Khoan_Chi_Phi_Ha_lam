using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CuttingThickness;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using CuttingThicknessEntity = Domain.Entities.Index.CuttingThickness;

namespace Application.Catalog.Index.CuttingThickness.Commands;

public record ImportCuttingThicknessExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportCuttingThicknessExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportCuttingThicknessExcelCommand, bool>
{
    private readonly IWriteRepository<CuttingThicknessEntity> _cuttingThicknessRepository = unitOfWork.GetRepository<CuttingThicknessEntity>();

    public async Task<bool> Handle(ImportCuttingThicknessExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<CuttingThicknessExcelDto>(stream);
        var excelDtos = dtos.Select(d => CuttingThicknessEntity.Create(d.Id, d.Value));
        var dbCuttingThickness = await _cuttingThicknessRepository.GetAllAsync(
            disableTracking: true);

        var deleteList = new List<CuttingThicknessEntity>();
        var updateList = new List<CuttingThicknessEntity>();
        var addList = new List<CuttingThicknessEntity>();

        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbCuttingThickness.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbCuttingThickness.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbCuttingThickness.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(CuttingThicknessEntity.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _cuttingThicknessRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _cuttingThicknessRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _cuttingThicknessRepository.Update(updateList);
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
