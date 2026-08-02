using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.TransportRoute;
using Application.Interfaces.Services;
using Domain.Entities.Index;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Index.TransportRoutes.Commands;

public record ImportTransportRouteExcelCommand(IFormFile File) : IRequest<bool>;

public class ImportTransportRouteExcelCommandHandler(IExcelService excelService, IUnitOfWork unitOfWork) : IRequestHandler<ImportTransportRouteExcelCommand, bool>
{
    private readonly IWriteRepository<TransportRoute> _transportRouteRepository = unitOfWork.GetRepository<TransportRoute>();
    private readonly IWriteRepository<Domain.Entities.Index.ProductionProcess> _productionProcessRepository = unitOfWork.GetRepository<Domain.Entities.Index.ProductionProcess>();

    public async Task<bool> Handle(ImportTransportRouteExcelCommand request, CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            throw new BadRequestException("Vui lòng chọn file Excel.");
        }

        using var stream = request.File.OpenReadStream();
        var excelDtos = excelService.ImportFromExcel<TransportRouteExcelDto>(stream);

        var productionProcesses = await _productionProcessRepository.GetAllAsync(
            include: query => query.Include(p => p.Code),
            disableTracking: true);

        var dbTransportRoutes = await _transportRouteRepository.GetAllAsync(
            include: query => query.Include(t => t.Code),
            disableTracking: true);

        var deleteList = new List<TransportRoute>();
        var updateList = new List<TransportRoute>();
        var addList = new List<TransportRoute>();

        var excelIds = excelDtos.Select(x => x.Id).Where(id => id != Guid.Empty).ToList();
        var entitiesToDelete = dbTransportRoutes.Where(x => !excelIds.Contains(x.Id)).ToList();
        deleteList.AddRange(entitiesToDelete);

        foreach (var dto in excelDtos)
        {
            if (string.IsNullOrWhiteSpace(dto.Code) || string.IsNullOrWhiteSpace(dto.Name))
            {
                continue;
            }

            var productionProcess = productionProcesses.FirstOrDefault(
                p => p.Code != null && p.Code.Value.ToUpper() == (dto.ProductionProcessCode ?? string.Empty).Trim().ToUpper());

            if (productionProcess == null)
            {
                throw new NotFoundException($"{CustomResponseMessage.ProductionProcessNotFound}: {dto.ProductionProcessCode}");
            }

            if (dto.Id != Guid.Empty && dbTransportRoutes.Any(x => x.Id == dto.Id))
            {
                var entityToUpdate = dbTransportRoutes.First(x => x.Id == dto.Id);

                var hasChanged = entityToUpdate.Code?.Value != dto.Code.Trim().ToUpper()
                    || entityToUpdate.Name != dto.Name.Trim()
                    || entityToUpdate.Note != dto.Note?.Trim()
                    || entityToUpdate.ProductionProcessId != productionProcess.Id
                    || entityToUpdate.IsSpecialLowVolume != dto.IsSpecialLowVolume;

                if (hasChanged)
                {
                    entityToUpdate.Update(dto.Code.Trim(), dto.Name.Trim(), dto.Note?.Trim(), productionProcess.Id, dto.IsSpecialLowVolume);
                    updateList.Add(entityToUpdate);
                }
            }
            else
            {
                addList.Add(TransportRoute.Create(dto.Code.Trim(), dto.Name.Trim(), dto.Note?.Trim(), productionProcess.Id, dto.IsSpecialLowVolume));
            }
        }

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            if (deleteList.Any())
            {
                _transportRouteRepository.Delete(deleteList);
            }

            if (addList.Any())
            {
                await _transportRouteRepository.InsertAsync(addList);
            }

            if (updateList.Any())
            {
                _transportRouteRepository.Update(updateList);
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