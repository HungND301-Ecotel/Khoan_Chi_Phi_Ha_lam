using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProcessGroup;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.ProcessGroups.Commands;

public record ImportProcessGroupExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportProcessGroupExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<ImportProcessGroupExcelCommand, bool>
{
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<bool> Handle(ImportProcessGroupExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var dtos = excelService.ImportFromExcel<ProcessGroupExcelDto>(stream);
        var excelDtos = dtos.Select(d => ProcessGroup.Create(d.Id, d.Code, d.Name));
        var dbProcessGroups = await _processGroupRepository.GetAllAsync(
            include: p => p.Include(p => p.Code!),
            disableTracking: true);

        var deleteList = new List<ProcessGroup>();
        var updateList = new List<ProcessGroup>();
        var addList = new List<ProcessGroup>();

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
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code?.Value ?? "", dto.Name);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                }

                addList.Add(ProcessGroup.Create(dto.Code?.Value ?? "", dto.Name));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _processGroupRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _processGroupRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _processGroupRepository.Update(updateList);
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