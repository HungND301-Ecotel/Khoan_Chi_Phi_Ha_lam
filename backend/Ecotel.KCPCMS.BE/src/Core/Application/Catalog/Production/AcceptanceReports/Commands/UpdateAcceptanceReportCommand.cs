using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.AcceptanceReports.Commands;

public record UpdateAcceptanceReportCommand(UpdateAcceptanceReportDto UpdateModel) : IRequest<UpdateAcceptanceReportResponseDto>;

public class UpdateAcceptanceReportCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAcceptanceReportCommand, UpdateAcceptanceReportResponseDto>
{
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();
    private readonly IWriteRepository<AcceptanceReportItem> _acceptanceReportItemRepository = unitOfWork.GetRepository<AcceptanceReportItem>();

    public async Task<UpdateAcceptanceReportResponseDto> Handle(UpdateAcceptanceReportCommand request, CancellationToken cancellationToken)
    {
        var updateModel = request.UpdateModel;

        // Validate AcceptanceReport exists
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.Id == updateModel.Id,
            include: q => q.Include(a => a.ProductionOutput)
                .ThenInclude(p => p.ProductionOutputProcessGroups),
            disableTracking: false);

        if (acceptanceReport == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Validate items not empty
        if (updateModel.Items == null || !updateModel.Items.Any())
        {
            throw new BadRequestException(CustomResponseMessage.UpdateIdsEmpty);
        }

        // Get ProductionOutput for validation
        var productionOutput = acceptanceReport.ProductionOutput;
        if (productionOutput == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        var processGroupIdsInPeriod = productionOutput.ProductionOutputProcessGroups
            .Select(x => x.ProcessGroupId)
            .ToHashSet();

        // Get existing items for comparison
        var existingItems = await _acceptanceReportItemRepository.GetAllAsync(
            predicate: i => i.AcceptanceReportId == updateModel.Id,
            disableTracking: false);

        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
        try
        {
            // Update AcceptanceReport
            acceptanceReport.Update(updateModel.FilePath);
            _acceptanceReportRepository.Update(acceptanceReport);
            await unitOfWork.SaveChangesAsync();

            // Group update model items by Id
            var itemsToUpdate = new Dictionary<Guid, UpdateAcceptanceReportItemDto>();
            foreach (var item in updateModel.Items)
            {
                itemsToUpdate[item.Id] = item;
            }

            // Delete items that are not in the update model
            var itemsToDelete = existingItems.Where(e => !itemsToUpdate.ContainsKey(e.Id)).ToList();
            if (itemsToDelete.Any())
            {
                foreach (var item in itemsToDelete)
                {
                    _acceptanceReportItemRepository.Delete(item);
                }
                await unitOfWork.SaveChangesAsync();
            }

            // Update existing items
            // MaterialId và PartId giữ nguyên từ entity, không cho phép thay đổi qua Update
            foreach (var existingItem in existingItems)
            {
                if (itemsToUpdate.TryGetValue(existingItem.Id, out var updateItem))
                {
                    existingItem.Update(
                        updateItem.ProcessGroupId,
                        existingItem.MaterialId,
                        existingItem.PartId,
                        updateItem.ItemType,
                        updateItem.ProductionOrderId,
                        updateItem.MaterialsIncludedInContractRevenue,
                        updateItem.MaterialsIncludedInContractRevenueQuantity,
                        updateItem.AdditionalCost,
                        updateItem.AdditionalCostQuantity,
                        updateItem.QuotaBasedMaterial,
                        updateItem.QuotaBasedMaterialType,
                        updateItem.QuotaBasedMaterialQuantity,
                        updateItem.Asset,
                        updateItem.AssetMaterialQuantity,
                        updateItem.IssuedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                        updateItem.ShippedDetails.Select(x => (x.Type, x.Quantity)).ToList());

                    if (updateItem.MaterialsIncludedInContractRevenue != Domain.Common.Enums.MaterialsIncludedInContractRevenue.None)
                    {
                        if (!updateItem.ProcessGroupId.HasValue || !processGroupIdsInPeriod.Contains(updateItem.ProcessGroupId.Value))
                        {
                            throw new NotFoundException(CustomResponseMessage.ProcessGroupNotFound);
                        }
                    }

                    _acceptanceReportItemRepository.Update(existingItem);
                }
            }

            if (itemsToUpdate.Any())
            {
                await unitOfWork.SaveChangesAsync();
            }

            await unitOfWork.CommitAsync(cancellationToken);

            return new UpdateAcceptanceReportResponseDto
            {
                Id = acceptanceReport.Id,
                ProductionOutputId = acceptanceReport.ProductionOutputId,
                FilePath = acceptanceReport.FilePath,
                ItemCount = itemsToUpdate.Count
            };
        }
        catch
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }
}