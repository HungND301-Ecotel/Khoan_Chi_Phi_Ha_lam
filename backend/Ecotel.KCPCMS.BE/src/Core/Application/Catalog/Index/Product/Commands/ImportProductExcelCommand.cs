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
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();
    public async Task<bool> Handle(ImportProductExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<ProductExcelDto>(stream);

        var dbProcessCodes = (await _processGroupRepository.GetAllAsync(
                include: p => p.Include(p => p.FixedKey),
                disableTracking: true))
            .Select(p => p.FixedKey?.Key?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < dtos.Count; i++)
        {
            var processGroupCode = dtos[i].ProcessGroupCode?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(processGroupCode) || !dbProcessCodes.Contains(processGroupCode))
            {
                throw new BadRequestException($"Giá trị mã nhóm công đoạn '{dtos[i].ProcessGroupCode}' không tồn tại ở dòng {i + 2}.");
            }

            if (dtos[i].StartMonth == default || dtos[i].EndMonth == default)
            {
                throw new BadRequestException($"Thời gian bắt đầu và thời gian kết thúc là bắt buộc ở dòng {i + 2}.");
            }
        }

        //Map data to Entity Model

        var processGroupCodes = dtos
            .Select(d => d.ProcessGroupCode?.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var processGroup = await _processGroupRepository.GetAllAsync(
            predicate: p => p.FixedKey != null && processGroupCodes.Contains(p.FixedKey.Key),
            include: p => p.Include(p => p.FixedKey),
            disableTracking: true);
        var processGroupIdMap = processGroup.ToDictionary(p => p.FixedKey!.Key.Trim(), p => p.Id, StringComparer.OrdinalIgnoreCase);

        var excelDtos = dtos.Select(d =>
        {
            if (processGroupIdMap.TryGetValue(d.ProcessGroupCode?.Trim() ?? string.Empty, out var processGroupId))
            {
                return ProductEntity.Create(
                    d.Id,
                    d.Code,
                    d.Name,
                    processGroupId,
                    d.StartMonth,
                    d.EndMonth);
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
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        for (var i = 0; i < excelDtos.Count; i++)
        {
            var dto = excelDtos[i];
            var rowNumber = i + 2;
            if (dto.Id != Guid.Empty && dbProduct.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbProduct.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    if (await codeService.IsCodeExisted(dto.Code.Value, entityToUpdate.Code.Id))
                    {
                        throw new ConflictException($"Giá trị mã '{dto.Code.Value}' đã tồn tại ở dòng {rowNumber}.");
                    }

                    entityToUpdate.Update(
                        dto.Code?.Value ?? "",
                        dto.Name,
                        dto.ProcessGroupId,
                        dto.StartMonth,
                        dto.EndMonth);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code.Value))
                {
                    throw new ConflictException($"Giá trị mã '{dto.Code.Value}' đã tồn tại ở dòng {rowNumber}.");
                }

                addList.Add(ProductEntity.Create(
                    dto.Code?.Value ?? "",
                    dto.Name,
                    dto.ProcessGroupId,
                    dto.StartMonth,
                    dto.EndMonth));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _productRepository.Delete(deleteList);

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
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

}
