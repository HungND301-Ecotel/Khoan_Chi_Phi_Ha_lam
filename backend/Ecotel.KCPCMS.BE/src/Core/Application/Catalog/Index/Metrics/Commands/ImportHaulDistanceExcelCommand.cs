using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Metric;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Index.Metrics.Commands;

public record ImportHaulDistanceExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportHaulDistanceExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportHaulDistanceExcelCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.HaulDistance> _repository =
        unitOfWork.GetRepository<Domain.Entities.Index.HaulDistance>();

    public async Task<bool> Handle(ImportHaulDistanceExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<HaulDistanceExcelDto>(stream).ToList();

        var dbHaulDistances = await _repository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var excelIds = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        var deleteList = dbHaulDistances.Where(x => !excelIds.Contains(x.Id)).ToList();
        var updateList = new List<Domain.Entities.Index.HaulDistance>();
        var addList = new List<Domain.Entities.Index.HaulDistance>();

        foreach (var dto in dtos)
        {
            var rowNumber = dtos.IndexOf(dto) + 2;

            if (string.IsNullOrWhiteSpace(dto.Value))
            {
                throw new BadRequestException($"Cung độ vận tải không được để trống ở dòng {rowNumber}.");
            }

            var isValueTakenByOther = dbHaulDistances.Any(x =>
                x.Value.Trim().Equals(dto.Value.Trim(), StringComparison.OrdinalIgnoreCase)
                && (!dto.Id.HasValue || x.Id != dto.Id.Value));

            if (isValueTakenByOther)
            {
                throw new ConflictException($"Cung độ vận tải '{dto.Value}' đã tồn tại ở dòng {rowNumber}.");
            }

            if (dto.Id.HasValue && dbHaulDistances.Any(x => x.Id == dto.Id.Value))
            {
                var entityToUpdate = dbHaulDistances.First(x => x.Id == dto.Id.Value);

                if (entityToUpdate.Value != dto.Value.Trim())
                {
                    entityToUpdate.Update(dto.Value.Trim());
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(Domain.Entities.Index.HaulDistance.Create(dto.Value.Trim()));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Count > 0)
            {
                _repository.Delete(deleteList);
            }

            if (addList.Count > 0)
            {
                await _repository.InsertAsync(addList);
            }

            if (updateList.Count > 0)
            {
                _repository.Update(updateList);
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