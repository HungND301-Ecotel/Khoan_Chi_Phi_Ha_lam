using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOrder;
using Application.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;
using ProductionOrderEntity = Domain.Entities.Index.ProductionOrder;

namespace Application.Catalog.Index.ProductionOrder.Commands;

public record ImportProductionOrderExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportProductionOrderExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork, ICodeService codeService) : IRequestHandler<ImportProductionOrderExcelCommand, bool>
{
    private readonly IWriteRepository<ProductionOrderEntity> _productionOrderRepository = unitOfWork.GetRepository<ProductionOrderEntity>();
    private readonly IWriteRepository<Domain.Entities.Index.Code> _codeRepository = unitOfWork.GetRepository<Domain.Entities.Index.Code>();

    public async Task<bool> Handle(ImportProductionOrderExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var dtos = excelService.ImportFromExcel<ProductionOrderExcelDto>(stream);

        var dbProductionOrders = await _productionOrderRepository.GetAllAsync(
            include: p => p.Include(p => p.Code),
            disableTracking: false);

        var dbProductionOrderDict = dbProductionOrders.ToDictionary(p => p.Id);

        var deleteList = new List<ProductionOrderEntity>();
        var updateList = new List<ProductionOrderEntity>();
        var addList = new List<ProductionOrderEntity>();

        var excelIds = dtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbProductionOrders.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);
        var codeToDelete = deleteList.Where(x => x.Code != null).Select(x => x.Code!).ToList();

        for (var i = 0; i < dtos.Count; i++)
        {
            var dto = dtos[i];
            var rowNumber = i + 2;
            if (dto.Id != Guid.Empty && dbProductionOrderDict.TryGetValue(dto.Id, out var entityToUpdate))
            {
                var isChanged =
                    !string.Equals(entityToUpdate.Code?.Value, dto.Code, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(entityToUpdate.Name, dto.Name, StringComparison.Ordinal)
                    || entityToUpdate.StartMonth != dto.StartMonth
                    || entityToUpdate.EndMonth != dto.EndMonth;

                if (isChanged)
                {
                    if (await codeService.IsCodeExisted(dto.Code, entityToUpdate.CodeId))
                    {
                        throw new ConflictException($"Giá trị mã '{dto.Code}' đã tồn tại ở dòng {rowNumber}.");
                    }

                    entityToUpdate.Update(dto.Code, dto.Name, dto.StartMonth, dto.EndMonth);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                if (await codeService.IsCodeExisted(dto.Code))
                {
                    throw new ConflictException($"Giá trị mã '{dto.Code}' đã tồn tại ở dòng {rowNumber}.");
                }

                addList.Add(ProductionOrderEntity.Create(dto.Code, dto.Name, dto.StartMonth, dto.EndMonth));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _productionOrderRepository.Delete(deleteList);

                if (codeToDelete.Any())
                {
                    _codeRepository.Delete(codeToDelete);
                }
            }

            if (addList.Any())
            {
                await _productionOrderRepository.InsertAsync(addList, cancellationToken);
            }

            if (updateList.Any())
            {
                _productionOrderRepository.Update(updateList);
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
