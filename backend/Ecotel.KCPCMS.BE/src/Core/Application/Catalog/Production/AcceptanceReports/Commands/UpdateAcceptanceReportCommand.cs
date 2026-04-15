using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.AcceptanceReport;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
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
    private readonly IWriteRepository<Part> _partRepository = unitOfWork.GetRepository<Part>();
    private readonly IWriteRepository<MaintainUnitPriceEquipment> _maintainUnitPriceEquipmentRepository = unitOfWork.GetRepository<MaintainUnitPriceEquipment>();

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

        var allParts = await _partRepository.GetAllAsync(disableTracking: true);
        var allMaintainUnitPriceEquipments = await _maintainUnitPriceEquipmentRepository.GetAllAsync(
            include: q => q.Include(m => m.MaintainUnitPrice),
            disableTracking: true);

        // Get existing items for comparison (include QuotaBasedMaterialQuantities for EF tracking)
        var existingItems = await _acceptanceReportItemRepository.GetAllAsync(
            predicate: i => i.AcceptanceReportId == updateModel.Id,
            include: q => q.Include(i => i.IssuedDetails)
                           .Include(i => i.ShippedDetails)
                           .Include(i => i.QuotaBasedMaterialQuantities),
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
            foreach (var existingItem in existingItems)
            {
                if (itemsToUpdate.TryGetValue(existingItem.Id, out var updateItem))
                {
                    var categoryReference = ProductionReference.Create(
                        updateItem.CategoryProductionOrderId,
                        updateItem.CategoryEquipmentId);
                    var additionalCostReference = ProductionReference.Create(
                        updateItem.AdditionalCostProductionOrderId,
                        updateItem.AdditionalCostEquipmentId);

                    Guid? resolvedMaintainUnitPriceEquipmentId = existingItem.MaintainUnitPriceEquipmentId;

                    if (existingItem.PartId.HasValue)
                    {
                        var part = allParts.FirstOrDefault(p => p.Id == existingItem.PartId.Value);
                        if (part?.Type == PartType.Part)
                        {
                            resolvedMaintainUnitPriceEquipmentId = ResolveMaintainUnitPriceEquipmentId(
                                existingItem.PartId.Value,
                                categoryReference.EquipmentId,
                                productionOutput.StartMonth,
                                productionOutput.EndMonth,
                                allMaintainUnitPriceEquipments);
                        }
                        else
                        {
                            resolvedMaintainUnitPriceEquipmentId = null;
                        }
                    }

                    existingItem.Update(
                        updateItem.ProcessGroupId,
                        existingItem.MaterialId,
                        existingItem.PartId,
                        resolvedMaintainUnitPriceEquipmentId,
                        updateItem.ItemType,
                        categoryReference,
                        additionalCostReference,
                        updateItem.MaterialsIncludedInContractRevenue,
                        updateItem.MaterialsIncludedInContractRevenueQuantity,
                        updateItem.AdditionalCost,
                        updateItem.OtherMaterialDetail,
                        updateItem.AdditionalCostQuantity,
                        updateItem.QuotaBasedMaterial,
                        updateItem.QuotaBasedMaterialType,
                        updateItem.Asset,
                        updateItem.AssetMaterialQuantity,
                        updateItem.IssuedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                        updateItem.ShippedDetails.Select(x => (x.Type, x.Quantity)).ToList(),
                        updateItem.QuotaBasedMaterialQuantities?.Select(x => (x.Type, x.Quantity)).ToList());

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

    private static Guid? ResolveMaintainUnitPriceEquipmentId(
        Guid partId,
        Guid? equipmentId,
        DateOnly startMonth,
        DateOnly endMonth,
        IEnumerable<MaintainUnitPriceEquipment> maintainItems)
    {
        if (!equipmentId.HasValue)
        {
            return null;
        }

        return maintainItems
            .Where(m => m.PartId == partId
                        && m.MaintainUnitPrice != null
                        && m.MaintainUnitPrice.EquipmentId == equipmentId.Value
                        && m.MaintainUnitPrice.StartMonth <= startMonth
                        && m.MaintainUnitPrice.EndMonth >= endMonth)
            .OrderByDescending(m => m.MaintainUnitPrice!.StartMonth)
            .Select(m => (Guid?)m.Id)
            .FirstOrDefault();
    }
}

