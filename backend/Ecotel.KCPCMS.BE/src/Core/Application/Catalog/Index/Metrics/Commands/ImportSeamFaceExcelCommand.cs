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

public record ImportSeamFaceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportSeamFaceExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportSeamFaceExcelCommand, bool>
{
    private readonly IWriteRepository<SeamFace> _seamFaceRepository = unitOfWork.GetRepository<SeamFace>();
    public async Task<bool> Handle(ImportSeamFaceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<SeamFaceExcelDto>(stream)).Adapt<List<SeamFace>>();
        var dbSeamFace = await _seamFaceRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<SeamFace>();
        var updateList = new List<SeamFace>();
        var addList = new List<SeamFace>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbSeamFace.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbSeamFace.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbSeamFace.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(SeamFace.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _seamFaceRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _seamFaceRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _seamFaceRepository.Update(updateList);
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
