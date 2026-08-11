using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.CargoType;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.CargoType.Commands;

public record ImportCargoTypeExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportCargoTypeExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork) : IRequestHandler<ImportCargoTypeExcelCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.CargoType> _haulDistanceRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.CargoType>();

    public async Task<bool> Handle(ImportCargoTypeExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<CargoTypeExcelDto>(stream).ToList();

        var dbHaulDistances = await _haulDistanceRepository.GetAllAsync(
            predicate: _ => true,
            include: x => x.Include(h => h.Code),
            disableTracking: true);

        var excelIds = dtos.Where(d => d.Id.HasValue).Select(d => d.Id!.Value).ToList();
        var deleteList = dbHaulDistances.Where(x => !excelIds.Contains(x.Id)).ToList();
        var updateList = new List<Domain.Entities.Index.CargoType>();
        var addList = new List<Domain.Entities.Index.CargoType>();

        foreach (var dto in dtos)
        {
            var rowNumber = dtos.IndexOf(dto) + 2;

            if (string.IsNullOrWhiteSpace(dto.Code))
            {
                throw new BadRequestException($"Mã chủng loại hàng không được để trống ở dòng {rowNumber}.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new BadRequestException($"Tên chủng loại hàng không được để trống ở dòng {rowNumber}.");
            }

            var isCodeTakenByOther = dbHaulDistances.Any(x =>
                x.Code != null
                && x.Code.Value.Trim().Equals(dto.Code.Trim(), StringComparison.OrdinalIgnoreCase)
                && (!dto.Id.HasValue || x.Id != dto.Id.Value));

            if (isCodeTakenByOther)
            {
                throw new ConflictException($"Mã chủng loại hàng '{dto.Code}' đã tồn tại ở dòng {rowNumber}.");
            }

            if (dto.Id.HasValue && dbHaulDistances.Any(x => x.Id == dto.Id.Value))
            {
                var entityToUpdate = dbHaulDistances.First(x => x.Id == dto.Id.Value);

                var hasChanged = entityToUpdate.Code?.Value != dto.Code.Trim().ToUpper()
                    || entityToUpdate.Name != dto.Name.Trim()
                    || entityToUpdate.Note != dto.Note;

                if (hasChanged)
                {
                    entityToUpdate.Update(dto.Code.Trim(), dto.Name.Trim(), dto.Note);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(Domain.Entities.Index.CargoType.Create(dto.Code.Trim(), dto.Name.Trim(), dto.Note));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Count > 0)
            {
                _haulDistanceRepository.Delete(deleteList);
            }

            if (addList.Count > 0)
            {
                await _haulDistanceRepository.InsertAsync(addList);
            }

            if (updateList.Count > 0)
            {
                _haulDistanceRepository.Update(updateList);
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