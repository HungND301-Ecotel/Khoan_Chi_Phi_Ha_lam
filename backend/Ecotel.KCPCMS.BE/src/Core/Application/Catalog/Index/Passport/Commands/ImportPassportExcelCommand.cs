using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Passport;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using PassportEntity = Domain.Entities.Index.Passport;

namespace Application.Catalog.Index.Passport.Commands;

public record ImportPassportExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportPassportExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportPassportExcelCommand, bool>
{
    private readonly IWriteRepository<PassportEntity> _passportRepository = unitOfWork.GetRepository<PassportEntity>();
    public async Task<bool> Handle(ImportPassportExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<PassportExcelDto>(stream);
        var excelDtos = dtos.Select(d => PassportEntity.Create(d.Id, d.Name, d.Sd, d.Sc));
        var dbProcessGroups = await _passportRepository.GetAllAsync(
            disableTracking: true);

        var deleteList = new List<PassportEntity>();
        var updateList = new List<PassportEntity>();
        var addList = new List<PassportEntity>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbProcessGroups.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbProcessGroups.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbProcessGroups.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Name, dto.Sd, dto.Sc);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(PassportEntity.Create(dto.Name, dto.Sd, dto.Sc));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _passportRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _passportRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _passportRepository.Update(updateList);
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