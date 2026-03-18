using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.Product;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using ProductEntity = Domain.Entities.Index.Product;

namespace Application.Catalog.Index.Product.Commands;

public record ImportProductExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportProductExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<ImportProductExcelCommand, bool>
{
    private readonly IWriteRepository<ProductEntity> _productRepository = unitOfWork.GetRepository<ProductEntity>();
    private readonly IWriteRepository<ProcessGroup> _processGroupRepository = unitOfWork.GetRepository<ProcessGroup>();
    public async Task<bool> Handle(ImportProductExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<ProductExcelDto>(stream);

        if (!(await CheckExistedProductionProcess(dtos)))
        {
            throw new BadRequestException(CustomResponseMessage.ProcessGroupNotFound);
        }

        //Map data to Entity Model

        var processGroup = await _processGroupRepository.GetAllAsync(
            predicate: p => dtos.Select(d => d.ProcessGroupCode).Contains(p.Code.Value),
            include: p => p.Include(p => p.Code!),
            disableTracking: true);
        var processGroupIdMap = processGroup.ToDictionary(p => p.Code.Value, p => p.Id);

        var excelDtos = dtos.Select(d =>
        {
            if (processGroupIdMap.TryGetValue(d.ProcessGroupCode, out var processGroupId))
            {
                return ProductEntity.Create(d.Id, d.Code, d.Name, processGroupId);
            }
            else
            {
                return null;
            }
        }).Where(d => d != null).ToList();


        var dbProduct = await _productRepository.GetAllAsync(
            include: p => p.Include(p => p.Code),
            disableTracking: true);

        var deleteList = new List<ProductEntity>();
        var updateList = new List<ProductEntity>();
        var addList = new List<ProductEntity>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbProduct.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbProduct.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbProduct.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                    }

                    entityToUpdate.Update(dto.Code?.Value ?? "", dto.Name, dto.ProcessGroupId);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    throw new ConflictException(CustomResponseMessage.CodeAlreadyExists);
                }

                addList.Add(ProductEntity.Create(dto.Code?.Value ?? "", dto.Name, dto.ProcessGroupId));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _productRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _productRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _productRepository.Update(updateList);
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

    private async Task<bool> CheckExistedProductionProcess(List<ProductExcelDto> dtoList)
    {
        var dbProcessCodes = (await _processGroupRepository.GetAllAsync(
                include: p => p.Include(p => p.Code),
                disableTracking: true))
            .Select(p => p.Code?.Value?.Trim())
            .Where(code => code != null)
            .ToHashSet();

        var excelProcessCodes = dtoList
            .Select(d => d.ProcessGroupCode?.Trim())
            .Where(code => !string.IsNullOrEmpty(code))
            .Distinct();

        return excelProcessCodes.All(code => dbProcessCodes.Contains(code));
    }
}