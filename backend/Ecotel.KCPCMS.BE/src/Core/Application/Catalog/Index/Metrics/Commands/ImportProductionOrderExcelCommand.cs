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

public record ImportProductionOrderExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportProductionOrderExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportProductionOrderExcelCommand, bool>
{
    private readonly IWriteRepository<ProductionOrder> _productionOrderRepository = unitOfWork.GetRepository<ProductionOrder>();
    public async Task<bool> Handle(ImportProductionOrderExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();

        var excelDtos = (excelService.ImportFromExcel<ProductioOrderExcelDto>(stream)).Adapt<List<ProductionOrder>>();
        var dbProductionOrder = await _productionOrderRepository.GetAllAsync(disableTracking: true);

        var deleteList = new List<ProductionOrder>();
        var updateList = new List<ProductionOrder>();
        var addList = new List<ProductionOrder>();

        //CheckDelete
        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbProductionOrder.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (dto.Id != Guid.Empty && dbProductionOrder.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbProductionOrder.First(x => x.Id == dto.Id);

                if (entityToUpdate.CheckChange(dto))
                {
                    entityToUpdate.Update(dto.Value);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(ProductionOrder.Create(dto.Value));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _productionOrderRepository.Delete(deleteList);
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