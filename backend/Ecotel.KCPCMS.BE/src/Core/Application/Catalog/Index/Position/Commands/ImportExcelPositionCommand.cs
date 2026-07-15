using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Position;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.Catalog.Index.Position.Commands;

public record ImportExcelPositionCommand(IFormFile File) : IRequest<bool>;

public class ImportExcelPositionCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportExcelPositionCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Position> _positionRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Position>();

    public async Task<bool> Handle(ImportExcelPositionCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<PositionExcelDto>(stream).ToList();

        var dbPositions = await _positionRepository.GetAllAsync(predicate: _ => true, disableTracking: true);

        var excelIds = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        var deleteList = dbPositions.Where(x => !excelIds.Contains(x.Id)).ToList();
        var updateList = new List<Domain.Entities.Index.Position>();
        var addList = new List<Domain.Entities.Index.Position>();

        foreach (var dto in dtos)
        {
            var rowNumber = dtos.IndexOf(dto) + 2;

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BadRequestException($"Tên chức vụ không được để trống ở dòng {rowNumber}.");
            }

            var isNameTakenByOther = dbPositions.Any(x =>
                x.Name.Trim().Equals(dto.Name.Trim(), StringComparison.OrdinalIgnoreCase)
                && (!dto.Id.HasValue || x.Id != dto.Id.Value));

            if (isNameTakenByOther)
            {
                throw new ConflictException($"Tên chức vụ '{dto.Name}' đã tồn tại ở dòng {rowNumber}.");
            }


            if (dto.Id.HasValue && dbPositions.Any(x => x.Id == dto.Id.Value))
            {
                var entityToUpdate = dbPositions.First(x => x.Id == dto.Id.Value);

                var hasChanged = entityToUpdate.Name != dto.Name.Trim()
                    || entityToUpdate.Level != dto.Level
                    || entityToUpdate.Description != dto.Description;

                if (hasChanged)
                {
                    entityToUpdate.Update(dto.Name.Trim(), dto.Level, dto.Description ?? string.Empty);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(Domain.Entities.Index.Position.Create(dto.Name.Trim(), dto.Level, dto.Description ?? string.Empty));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Count > 0)
            {
                _positionRepository.Delete(deleteList);
            }

            if (addList.Count > 0)
            {
                await _positionRepository.InsertAsync(addList);
            }

            if (updateList.Count > 0)
            {
                _positionRepository.Update(updateList);
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