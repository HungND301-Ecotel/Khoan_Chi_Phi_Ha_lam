using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Department;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Catalog.Index.Department.Commands;

public record ImportDepartmentExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportDepartmentExcelCommandHandler(
    IExcelService excelService,
    IUnitOfWork unitOfWork,
    ICodeService codeService) : IRequestHandler<ImportDepartmentExcelCommand, bool>
{
    private readonly IWriteRepository<Domain.Entities.Index.Department> _departmentRepository =
        unitOfWork.GetRepository<Domain.Entities.Index.Department>();
    private readonly IWriteRepository<Code> _codeRepository = unitOfWork.GetRepository<Code>();

    public async Task<bool> Handle(ImportDepartmentExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<DepartmentExcelDto>(stream);
        var excelDtos = dtos.Select(d => Domain.Entities.Index.Department.Create(d.Id, d.Code, d.Name)).ToList();
        var dbDepartments = await _departmentRepository.GetAllAsync(
            include: q => q.Include(d => d.Code!),
            disableTracking: true);

        var deleteList = new List<Domain.Entities.Index.Department>();
        var updateList = new List<Domain.Entities.Index.Department>();
        var addList = new List<Domain.Entities.Index.Department>();

        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbDepartments.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        var codeToDelete = deleteList
            .Where(x => x.Code != null)
            .Select(x => x.Code!)
            .ToList();

        var rowById = dtos
            .Where(d => d.Id != Guid.Empty)
            .GroupBy(d => d.Id)
            .ToDictionary(g => g.Key, g => g.Select(x => dtos.IndexOf(x) + 2).First());
        var rowByCode = dtos
            .Where(d => !string.IsNullOrWhiteSpace(d.Code))
            .GroupBy(d => d.Code.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => dtos.IndexOf(x) + 2).First(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbDepartments.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbDepartments.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    var codeValue = dto.Code?.Value ?? string.Empty;
                    if (await codeService.IsCodeExisted(codeValue, entityToUpdate.CodeId))
                    {
                        var rowNumber = rowById.TryGetValue(dto.Id, out var byId)
                            ? byId
                            : (rowByCode.TryGetValue(codeValue, out var byCode) ? byCode : 2);
                        throw new ConflictException($"Giá trị mã '{codeValue}' đã tồn tại ở dòng {rowNumber}.");
                    }

                    entityToUpdate.Update(codeValue, dto.Name);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                var codeValue = dto.Code?.Value ?? string.Empty;
                if (await codeService.IsCodeExisted(codeValue))
                {
                    var rowNumber = rowByCode.TryGetValue(codeValue, out var byCode) ? byCode : 2;
                    throw new ConflictException($"Giá trị mã '{codeValue}' đã tồn tại ở dòng {rowNumber}.");
                }

                addList.Add(Domain.Entities.Index.Department.Create(codeValue, dto.Name));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _departmentRepository.Delete(deleteList);

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _departmentRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _departmentRepository.Update(updateList);
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
