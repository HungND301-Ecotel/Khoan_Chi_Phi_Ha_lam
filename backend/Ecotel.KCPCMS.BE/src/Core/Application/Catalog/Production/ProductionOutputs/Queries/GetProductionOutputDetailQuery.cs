using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Dto.Catalog.ProductionOutput;
using Domain.Common.Enums;
using Domain.Entities.Index;
using Domain.Entities.Production;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Constants;

namespace Application.Catalog.Production.ProductionOutputs.Queries;

public record GetProductionOutputDetailQuery(Guid ProductionOutputId) : IRequest<ProductionOutputDetailResponseDto>;

public class GetProductionOutputDetailQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetProductionOutputDetailQuery, ProductionOutputDetailResponseDto>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository =
        unitOfWork.GetRepository<ProductionOutput>();

    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository =
        unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<ProductionOutputDetailResponseDto> Handle(
        GetProductionOutputDetailQuery request, CancellationToken cancellationToken)
    {
        var productionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.ProductionOutputId,
            disableTracking: true)
            ?? throw new NotFoundException(CustomResponseMessage.EntityNotFound);

        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.ProductionOutputId == request.ProductionOutputId,
            include: q => q
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material).ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material).ThenInclude(m => m.AssignmentCode).ThenInclude(ac => ac.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material).ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material).ThenInclude(m => m.Costs)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Part).ThenInclude(p => p.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Part).ThenInclude(p => p.EquipmentParts)
                    .ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
                .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Part).ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Part).ThenInclude(p => p.Costs)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.IssuedDetails)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.ShippedDetails)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.AcceptanceReportItemLogs)
                .Include(a => a.ProductionOutput),
            disableTracking: true);

        var sectionA = new List<MaterialGroupDto>();
        var sectionB = new List<MaterialGroupDto>();
        var sectionC = new List<MaterialGroupDto>();
        var sectionD = new List<MaterialGroupDto>();

        if (acceptanceReport != null)
        {
            var previousReports = await GetPreviousReportsAsync(productionOutput);

            sectionA = BuildSectionA(acceptanceReport, previousReports, productionOutput);
            sectionB = BuildSectionB(acceptanceReport, previousReports, productionOutput);
            sectionC = BuildSectionC(acceptanceReport, productionOutput);
            sectionD = BuildSectionD(acceptanceReport, productionOutput);
        }

        return new ProductionOutputDetailResponseDto
        {
            ProductionOutputId = productionOutput.Id,
            StartMonth = productionOutput.StartMonth,
            EndMonth = productionOutput.EndMonth,
            ProductionMeters = productionOutput.ProductionMeters,
            StandardProductionMeters = productionOutput.StandardProductionMeters,
            SectionA = sectionA,
            SectionB = sectionB,
            SectionC = sectionC,
            SectionD = sectionD,
        };
    }

    // =========================================================================
    // SECTION A — Vật tư tính vào doanh thu khoán
    //
    //  Sub-section 1 (SectionAType=1): Vật liệu
    //    Group theo AssignmentCode | "VTK"
    //
    //  Sub-section 2 (SectionAType=2): SCTX TH1 — lĩnh kỳ này
    //    PartType=OtherPart               → group "VTK"
    //    Có ProductionOrderId             → group theo ProductionOrderId
    //    Không có ProductionOrderId       → group theo Equipment.Code
    //
    //  Sub-section 3 (SectionAType=3): SCTX TH2 — chi phí dài kỳ phân bổ
    //    Thuộc kỳ trước + có ProductionOrderId → group theo ProductionOrderId
    //    Còn lại                               → group theo Equipment.Code | "VTK"
    // =========================================================================
    private List<MaterialGroupDto> BuildSectionA(
        AcceptanceReport report,
        List<AcceptanceReport> previousReports,
        ProductionOutput productionOutput)
    {
        var groups = new Dictionary<string, MaterialGroupDto>();

        var sectionItems = report.AcceptanceReportItems
            .Where(i => i.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
            .ToList();

        // ── Sub-section 1: Vật liệu ──────────────────────────────────────────
        foreach (var item in sectionItems.Where(i => i.MaterialId.HasValue && i.Material != null))
        {
            var groupKey = $"A1_{item.Material!.AssignmentCode?.Code?.Value ?? "VTK"}";
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = item.Material.AssignmentCode?.Code?.Value ?? "VTK",
                GroupName = item.Material.AssignmentCode?.Name ?? "Vật liệu khác",
                MaterialType = MatTypeLabel.VatLieu,
                SectionAType = SecAType.VatLieu,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Material.Costs, productionOutput.StartMonth);
            group.Materials.Add(BuildVatLieuDetail(item, item.Material, plannedPrice, actualPrice));
        }

        // ── Sub-section 2: SCTX TH1 ──────────────────────────────────────────
        foreach (var item in sectionItems.Where(i => i.PartId.HasValue && i.Part != null))
        {
            var (groupKey, groupCode, groupName) = ResolveSctxGroupKey("A2", item);
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = MatTypeLabel.Sctx,
                SectionAType = SecAType.SctxTh1,
                ProductionOrderId = item.ProductionOrderId,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Part!.Costs, productionOutput.StartMonth);
            var th1Logs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId == report.Id)
                .ToList();

            foreach (var log in th1Logs)
            {
                group.Materials.Add(BuildSctxTh1Detail(item, item.Part, plannedPrice, actualPrice, log));
            }
        }

        // ── Sub-section 3: SCTX TH2 (từ current report — logs kỳ trước còn tồn) ──
        foreach (var item in sectionItems.Where(i => i.PartId.HasValue && i.Part != null))
        {
            var part = item.Part!;
            var oldLogs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId != report.Id && l.RemainingTime > 0)
                .OrderByDescending(l => l.PeriodEndMonth)
                .ToList();

            if (!oldLogs.Any())
            {
                continue;
            }

            var (groupKey, groupCode, groupName) = ResolveSctxTh2GroupKey("A3", item);
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = MatTypeLabel.Sctx,
                SectionAType = SecAType.SctxTh2,
                ProductionOrderId = item.ProductionOrderId,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(part.Costs, productionOutput.StartMonth);
            group.Materials.Add(BuildSctxTh2Detail(item, part, plannedPrice, actualPrice, oldLogs, productionOutput));
        }

        // SCTX TH2 từ previous reports
        foreach (var prevReport in previousReports)
        {
            var prevItems = prevReport.AcceptanceReportItems
                .Where(i => i.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
                         && i.PartId.HasValue && i.Part != null)
                .ToList();

            foreach (var item in prevItems)
            {
                var oldLogs = item.AcceptanceReportItemLogs
                    .Where(l => l.RemainingTime > 0)
                    .OrderByDescending(l => l.PeriodEndMonth)
                    .ToList();

                if (!oldLogs.Any())
                {
                    continue;
                }

                var (groupKey, groupCode, groupName) = ResolveSctxTh2GroupKey("A3", item);
                var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = MatTypeLabel.Sctx,
                    SectionAType = SecAType.SctxTh2,
                    ProductionOrderId = item.ProductionOrderId,
                    Materials = new(),
                    SubGroups = new()
                });

                var (plannedPrice, actualPrice) = GetUnitPrices(item.Part.Costs, productionOutput.StartMonth);
                group.Materials.Add(BuildSctxTh2Detail(item, item.Part, plannedPrice, actualPrice, oldLogs, productionOutput));
            }
        }

        return groups.Values
            .OrderBy(g => g.SectionAType)
            .ThenBy(g => g.GroupCode)
            .ToList();
    }

    // =========================================================================
    // SECTION B — Quyết định bổ sung chi phí
    //
    //  AdditionalCost = Material:
    //    Có ProductionOrderId → group theo ProductionOrderId
    //    Không có             → group "NO_ORDER" (flat)
    //
    //  AdditionalCost = Maintain (SCTX):
    //    Giống SectionA SCTX TH1:
    //    PartType=OtherPart   → "VTK"
    //    Có ProductionOrder   → group theo ProductionOrderId
    //    Không có             → group theo Equipment.Code
    //
    //  AdditionalCost = OtherMaterial:
    //    Group theo OtherMaterialDetail enum
    // =========================================================================
    private List<MaterialGroupDto> BuildSectionB(
        AcceptanceReport report,
        List<AcceptanceReport> previousReports,
        ProductionOutput productionOutput)
    {
        var groups = new Dictionary<string, MaterialGroupDto>();

        var sectionItems = report.AcceptanceReportItems
            .Where(i => i.AdditionalCost != AdditionalCost.None)
            .ToList();

        // ── Vật liệu ─────────────────────────────────────────────────────────
        foreach (var item in sectionItems
            .Where(i => i.AdditionalCost == AdditionalCost.Material
                     && i.MaterialId.HasValue && i.Material != null))
        {
            string groupCode, groupName, groupKey;
            Guid? productionOrderId = null;

            if (item.AdditionalCostProductionOrderId.HasValue)
            {
                groupCode = item.AdditionalCostProductionOrderId.Value.ToString();
                groupName = groupCode; // FE tự resolve tên từ ProductionOrderId
                groupKey = $"BM_{groupCode}";
                productionOrderId = item.AdditionalCostProductionOrderId;
            }
            else
            {
                groupCode = "NO_ORDER";
                groupName = "";
                groupKey = "BM_NO_ORDER";
            }

            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = MatTypeLabel.VatLieu,
                AdditionalCostType = AdditionalCost.Material,
                ProductionOrderId = productionOrderId,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Material!.Costs, productionOutput.StartMonth);
            group.Materials.Add(BuildVatLieuDetail(item, item.Material, plannedPrice, actualPrice, true));
        }

        // ── SCTX TH1 ─────────────────────────────────────────────────────────
        foreach (var item in sectionItems
            .Where(i => i.AdditionalCost == AdditionalCost.Maintain
                     && i.PartId.HasValue && i.Part != null))
        {
            var (groupKey, groupCode, groupName) = ResolveSctxGroupKey("BS", item, true);
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = MatTypeLabel.Sctx,
                AdditionalCostType = AdditionalCost.Maintain,
                ProductionOrderId = item.AdditionalCostProductionOrderId,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Part!.Costs, productionOutput.StartMonth);
            var th1Logs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId == report.Id)
                .ToList();
            foreach (var log in th1Logs)
            {
                group.Materials.Add(BuildSctxTh1Detail(item, item.Part, plannedPrice, actualPrice, log, true));
            }

            var oldLogs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId != report.Id && l.RemainingTime > 0)
                .OrderByDescending(l => l.PeriodEndMonth)
                .ToList();
            if (oldLogs.Any())
            {
                group.Materials.Add(BuildSctxTh2Detail(item, item.Part, plannedPrice, actualPrice, oldLogs, productionOutput, true));
            }
        }

        // SCTX TH2 từ previous reports
        foreach (var prevReport in previousReports)
        {
            var prevItems = prevReport.AcceptanceReportItems
                .Where(i => i.AdditionalCost == AdditionalCost.Maintain
                         && i.PartId.HasValue && i.Part != null)
                .ToList();

            foreach (var item in prevItems)
            {
                var (groupKey, groupCode, groupName) = ResolveSctxGroupKey("BS", item, true);
                var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = MatTypeLabel.Sctx,
                    AdditionalCostType = AdditionalCost.Maintain,
                    ProductionOrderId = item.AdditionalCostProductionOrderId,
                    Materials = new(),
                    SubGroups = new()
                });

                var oldLogs = item.AcceptanceReportItemLogs
                    .Where(l => l.RemainingTime > 0)
                    .OrderByDescending(l => l.PeriodEndMonth)
                    .ToList();
                if (!oldLogs.Any())
                {
                    continue;
                }

                var (plannedPrice, actualPrice) = GetUnitPrices(item.Part!.Costs, productionOutput.StartMonth);
                group.Materials.Add(BuildSctxTh2Detail(item, item.Part, plannedPrice, actualPrice, oldLogs, productionOutput, true));
            }
        }

        // ── OtherMaterial ─────────────────────────────────────────────────────
        foreach (var item in sectionItems
            .Where(i => i.AdditionalCost == AdditionalCost.SafeAndWelfare
                     && i.MaterialId.HasValue && i.Material != null))
        {
            var groupKey = $"BO_{item.OtherMaterialDetail}";
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = item.OtherMaterialDetail.ToString()!,
                GroupName = "",
                MaterialType = MatTypeLabel.VatLieu,
                AdditionalCostType = AdditionalCost.SafeAndWelfare,
                OtherMaterialDetail = item.OtherMaterialDetail,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Material!.Costs, productionOutput.StartMonth);
            group.Materials.Add(BuildVatLieuDetail(item, item.Material, plannedPrice, actualPrice, true));
        }

        return groups.Values
            .OrderBy(g => (int)(g.AdditionalCostType ?? AdditionalCost.None))
            .ThenBy(g => g.GroupCode)
            .ToList();
    }

    // =========================================================================
    // SECTION C — Vật tư khoán theo hạn mức
    //   MineSupport / SupportAccessories → SubGroups: New / Reusable
    //   MineTimber                       → Materials trực tiếp
    // =========================================================================
    private List<MaterialGroupDto> BuildSectionC(
        AcceptanceReport report, ProductionOutput productionOutput)
    {
        var groups = new Dictionary<string, MaterialGroupDto>();

        var sectionItems = report.AcceptanceReportItems
            .Where(i => i.QuotaBasedMaterial != QuotaBasedMaterial.None
                     && i.MaterialId.HasValue && i.Material != null)
            .ToList();

        foreach (var item in sectionItems)
        {
            var (groupKey, groupName, hasSubGroup) = item.QuotaBasedMaterial switch
            {
                QuotaBasedMaterial.MineSupport => ("MineSupport", "Vì chống lò", true),
                QuotaBasedMaterial.SupportAccessories => ("SupportAccessories", "Phụ kiện", true),
                QuotaBasedMaterial.MineTimber => ("MineTimber", "Gỗ lò", false),
                _ => ("VTK", "Vật tư khác", false)
            };

            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupKey,
                GroupName = groupName,
                MaterialType = groupName,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(item.Material!.Costs, productionOutput.StartMonth);
            var detail = BuildQuotaBasedDetail(item, item.Material, plannedPrice, actualPrice);

            if (hasSubGroup)
            {
                var subCode = item.QuotaBasedMaterialType == QuotaBasedMaterialType.New ? "New" : "Reusable";
                var subGroup = group.SubGroups.FirstOrDefault(s => s.SubGroupCode == subCode);
                if (subGroup == null)
                {
                    subGroup = new SubGroupDto { SubGroupCode = subCode, Materials = new() };
                    group.SubGroups.Add(subGroup);
                }
                subGroup.Materials.Add(detail);
            }
            else
            {
                group.Materials.Add(detail);
            }
        }

        foreach (var g in groups.Values.Where(g => g.SubGroups.Any()))
        {
            var sorted = g.SubGroups.OrderBy(s => s.SubGroupCode == "New" ? 0 : 1).ToList();
            g.SubGroups.Clear();
            foreach (var s in sorted)
            {
                g.SubGroups.Add(s);
            }
        }

        return groups.Values
            .OrderBy(g => g.GroupCode == "MineSupport" ? 0
                        : g.GroupCode == "SupportAccessories" ? 1
                        : g.GroupCode == "MineTimber" ? 2 : 3)
            .ToList();
    }

    // =========================================================================
    // SECTION D — Tài sản
    // =========================================================================
    private List<MaterialGroupDto> BuildSectionD(
        AcceptanceReport report, ProductionOutput productionOutput)
    {
        var sectionItems = report.AcceptanceReportItems
            .Where(i => i.Asset != Asset.None && i.MaterialId.HasValue && i.Material != null)
            .ToList();

        if (sectionItems.Any())
        {
            var group = new MaterialGroupDto
            {
                GroupCode = "ASSET",
                GroupName = "Tài sản",
                MaterialType = "Tài sản",
                Materials = new(),
                SubGroups = new()
            };

            foreach (var item in sectionItems)
            {
                var (plannedPrice, actualPrice) = GetUnitPrices(item.Material!.Costs, productionOutput.StartMonth);
                group.Materials.Add(BuildVatLieuDetail(item, item.Material, plannedPrice, actualPrice));
            }

            return new() { group };
        }

        return new();
    }

    // =========================================================================
    // SCTX group key resolver
    //   PartType=OtherPart   → "VTK"
    //   Có ProductionOrder   → ProductionOrderId.ToString()
    //   Không có             → Equipment.Code
    // =========================================================================
    private static (string key, string code, string name) ResolveSctxGroupKey(
        string prefix, AcceptanceReportItem item, bool useAdditionalCostReference = false)
    {
        if (item.Part!.Type == PartType.OtherPart)
        {
            return ($"{prefix}_VTK", "VTK", "Vật tư khác");
        }

        var productionOrderId = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId
            : item.ProductionOrderId;
        if (productionOrderId.HasValue)
        {
            var id = productionOrderId.Value.ToString();
            return ($"{prefix}_PO_{id}", id, id); // FE resolve tên từ id
        }

        var equipment = ResolveEquipmentForGrouping(item, useAdditionalCostReference);
        var code = equipment?.Code?.Value ?? "VTK";
        var name = equipment?.Name ?? "Vật tư khác";
        return ($"{prefix}_EQ_{code}", code, name);
    }

    /// <summary>
    /// TH2 grouping rule:
    /// - Nếu có ProductionOrderId (chi phí kéo dài từ kỳ trước theo lệnh/quyết định) thì group theo ProductionOrderId.
    /// - Nếu không thì group theo Equipment.Code (fallback "VTK").
    /// </summary>
    private static (string key, string code, string name) ResolveSctxTh2GroupKey(
        string prefix, AcceptanceReportItem item, bool useAdditionalCostReference = false)
    {
        var productionOrderId = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId
            : item.ProductionOrderId;
        if (productionOrderId.HasValue)
        {
            var id = productionOrderId.Value.ToString();
            return ($"{prefix}_PO_{id}", id, id); // FE resolve tên từ id
        }

        var equipment = ResolveEquipmentForGrouping(item, useAdditionalCostReference);
        var code = equipment?.Code?.Value ?? "VTK";
        var name = equipment?.Name ?? "Vật tư khác";
        return ($"{prefix}_EQ_{code}", code, name);
    }

    private static Equipment? ResolveEquipmentForGrouping(
        AcceptanceReportItem item,
        bool useAdditionalCostReference)
    {
        if (!useAdditionalCostReference)
        {
            return item.Equipment ?? item.Part?.EquipmentParts?.FirstOrDefault()?.Equipment;
        }

        if (item.AdditionalCostEquipmentId.HasValue)
        {
            return item.Part?.EquipmentParts?
                .FirstOrDefault(ep => ep.EquipmentId == item.AdditionalCostEquipmentId.Value)?
                .Equipment;
        }

        return item.Part?.EquipmentParts?.FirstOrDefault()?.Equipment;
    }

    // =========================================================================
    // Detail builders
    // =========================================================================

    private static MaterialDetailDto BuildVatLieuDetail(
        AcceptanceReportItem item,
        Material material,
        decimal plannedPrice,
        decimal actualPrice,
        bool useAdditionalCostReference = false)
    {
        var issued = BuildIssuedInPeriod(item, plannedPrice, actualPrice);
        var exported = BuildExportedInPeriod(item, plannedPrice);
        var endingQty = item.IssuedQuantity - item.ShippedQuantity;
        var hasProductionOrder = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId.HasValue
            : item.ProductionOrderId.HasValue;

        return new MaterialDetailDto
        {
            MaterialId = material.Id,
            MaterialCode = material.Code?.Value ?? "",
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasure?.Name ?? "",
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            IssuedInPeriod = issued,
            ExportedInPeriod = exported,
            EndingInventory = new EndingInventoryDto
            {
                RemainingAtSite = !hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                RemainingByOrder = hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                Total = new TotalDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
            }
        };
    }

    private static MaterialDetailDto BuildSctxTh1Detail(
        AcceptanceReportItem item,
        Part part,
        decimal plannedPrice,
        decimal actualPrice,
        AcceptanceReportItemLog log,
        bool useAdditionalCostReference = false)
    {
        var issued = BuildIssuedInPeriod(item, plannedPrice, actualPrice);
        var exported = BuildExportedInPeriod(item, plannedPrice);
        var endingQty = item.IssuedQuantity - item.ShippedQuantity;
        var hasProductionOrder = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId.HasValue
            : item.ProductionOrderId.HasValue;

        // Keep TH1 behavior: total exported reflects long-term accounted this period.
        exported.LongTermExpense = new LongTermExpenseDto { Amount = log.AccountedValueThisPeriod };
        exported.Total = new TotalDto { Quantity = 0, Amount = log.AccountedValueThisPeriod };

        return new MaterialDetailDto
        {
            MaterialId = part.Id,
            MaterialCode = part.Code?.Value ?? "",
            MaterialName = part.Name,
            UnitOfMeasureName = part.UnitOfMeasure?.Name ?? "",
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            IssuedInPeriod = issued,
            ExportedInPeriod = exported,
            EndingInventory = new EndingInventoryDto
            {
                RemainingAtSite = !hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                RemainingByOrder = hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                Total = new TotalDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
            }
        };
    }

    private static MaterialDetailDto BuildSctxTh2Detail(
        AcceptanceReportItem item, Part part, decimal plannedPrice, decimal actualPrice,
        List<AcceptanceReportItemLog> oldLogs, ProductionOutput productionOutput,
        bool useAdditionalCostReference = false)
    {
        var latestLog = oldLogs.First();
        var pendingStart = latestLog.PendingValueEndPeriod;

        decimal accountedThisPeriod = 0;
        if (latestLog.UsageTime > 0 && productionOutput.StandardProductionMeters > 0)
        {
            var valueByStandard = (pendingStart / (decimal)latestLog.UsageTime)
                * ((decimal)productionOutput.ProductionMeters
                   / (decimal)productionOutput.StandardProductionMeters);
            accountedThisPeriod = Math.Min(pendingStart, valueByStandard * (decimal)latestLog.AllocationRatio);
        }

        var pendingEnd = pendingStart - accountedThisPeriod;
        var endingQty = item.IssuedQuantity - item.ShippedQuantity;
        var hasProductionOrder = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId.HasValue
            : item.ProductionOrderId.HasValue;

        // BeginningInventory: carry-forward từ tồn cuối kỳ tháng trước
        // Tách RemainingAtSite vs RemainingByOrder theo ProductionOrderId
        var beginningInventory = new BeginningInventoryDto
        {
            RemainingAtSite = !hasProductionOrder
                ? new InventoryQuantityDto { Quantity = 0, Amount = pendingStart }
                : null,
            RemainingByOrder = hasProductionOrder
                ? new InventoryQuantityDto { Quantity = 0, Amount = pendingStart }
                : null,
            PendingValue = pendingStart,
            Total = new TotalDto { Quantity = 0, Amount = pendingStart }
        };

        // EndingInventory: tách tương tự
        var endingInventory = new EndingInventoryDto
        {
            RemainingAtSite = !hasProductionOrder
                ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                : null,
            RemainingByOrder = hasProductionOrder
                ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                : null,
            PendingValue = pendingEnd,
            Total = new TotalDto { Quantity = endingQty, Amount = pendingEnd }
        };

        var issued = BuildIssuedInPeriod(item, plannedPrice, actualPrice);
        issued.Received = new ReceivedSuppliesDto { Quantity = 0, PlannedAmount = 0, ActualAmount = 0 };
        issued.Total = new TotalDto { Quantity = 0, Amount = 0 };

        var exported = BuildExportedInPeriod(item, plannedPrice);
        exported.LongTermExpense = new LongTermExpenseDto { Amount = accountedThisPeriod };
        exported.Total = new TotalDto { Quantity = 0, Amount = accountedThisPeriod };

        return new MaterialDetailDto
        {
            MaterialId = part.Id,
            MaterialCode = part.Code?.Value ?? "",
            MaterialName = part.Name,
            UnitOfMeasureName = part.UnitOfMeasure?.Name ?? "",
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            BeginningInventory = beginningInventory,
            IssuedInPeriod = issued,
            ExportedInPeriod = exported,
            EndingInventory = endingInventory
        };
    }

    private static MaterialDetailDto BuildQuotaBasedDetail(
        AcceptanceReportItem item, Material material, decimal plannedPrice, decimal actualPrice)
    {
        var endingQty = item.IssuedQuantity - item.ShippedQuantity;
        var hasProductionOrder = item.ProductionOrderId.HasValue;

        var issued = BuildIssuedInPeriod(item, plannedPrice, actualPrice);
        var exported = BuildExportedInPeriod(item, plannedPrice);

        return new MaterialDetailDto
        {
            MaterialId = material.Id,
            MaterialCode = material.Code?.Value ?? "",
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasure?.Name ?? "",
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            IssuedInPeriod = issued,
            ExportedInPeriod = exported,
            EndingInventory = new EndingInventoryDto
            {
                RemainingAtSite = !hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                RemainingByOrder = hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
                    : null,
                Total = new TotalDto { Quantity = endingQty, Amount = (decimal)endingQty * plannedPrice }
            }
        };
    }

    // =========================================================================
    // Utilities
    // =========================================================================

    private async Task<List<AcceptanceReport>> GetPreviousReportsAsync(ProductionOutput productionOutput)
    {
        try
        {
            var all = await _acceptanceReportRepository.GetAllAsync(
                include: q => q
                    .Include(a => a.ProductionOutput)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.Part).ThenInclude(p => p.Code)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.Part).ThenInclude(p => p.EquipmentParts)
                            .ThenInclude(ep => ep.Equipment).ThenInclude(e => e.Code)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.Part).ThenInclude(p => p.UnitOfMeasure)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.Part).ThenInclude(p => p.Costs)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.AcceptanceReportItemLogs),
                disableTracking: true);

            return all
                .Where(a => a.ProductionOutput != null
                         && a.ProductionOutput.StartMonth < productionOutput.StartMonth)
                .OrderByDescending(a => a.ProductionOutput.StartMonth)
                .ToList();
        }
        catch
        {
            return new();
        }
    }

    private static MaterialGroupDto GetOrAddGroup(
        Dictionary<string, MaterialGroupDto> dict,
        string key,
        MaterialGroupDto newGroup)
    {
        if (!dict.ContainsKey(key))
        {
            dict[key] = newGroup;
        }

        return dict[key];
    }

    private static IssuedInPeriodDto BuildIssuedInPeriod(
        AcceptanceReportItem item,
        decimal plannedUnitPrice,
        decimal actualUnitPrice)
    {
        var receiptQty = GetIssuedQuantity(item, IssuedQuantityType.LinhVatTuTraPhieu);
        var borrowedQty = GetIssuedQuantity(item, IssuedQuantityType.VayVhuaTraPhieu);
        var returnPrevQty = GetIssuedQuantity(item, IssuedQuantityType.TraPhieuThangTruoc);
        var otherQty = GetIssuedQuantity(item, IssuedQuantityType.LinhKhac);

        return new IssuedInPeriodDto
        {
            Received = new ReceivedSuppliesDto
            {
                Quantity = receiptQty,
                PlannedAmount = (decimal)receiptQty * plannedUnitPrice,
                ActualAmount = (decimal)receiptQty * actualUnitPrice
            },
            BorrowedNoVoucher = new QuantityAmountDto
            {
                Quantity = borrowedQty,
                Amount = (decimal)borrowedQty * plannedUnitPrice
            },
            ReturnPreviousMonthVoucher = new QuantityAmountDto
            {
                Quantity = returnPrevQty,
                Amount = (decimal)returnPrevQty * plannedUnitPrice
            },
            OtherReceipt = new QuantityAmountDto
            {
                Quantity = otherQty,
                Amount = (decimal)otherQty * plannedUnitPrice
            },
            Total = new TotalDto
            {
                Quantity = item.IssuedQuantity,
                Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
            }
        };
    }

    private static ExportedInPeriodDto BuildExportedInPeriod(
        AcceptanceReportItem item,
        decimal plannedUnitPrice)
    {
        var productionQty = GetShippedQuantity(item, ShippedQuantityType.XuatChoSanXuat);
        var otherQty = GetShippedQuantity(item, ShippedQuantityType.XuatKhac);
        var contractQty = GetShippedQuantity(item, ShippedQuantityType.QuyetToanGiaoKhoan);

        return new ExportedInPeriodDto
        {
            ExportedToProduction = new ExportedToProductionDto
            {
                Quantity = productionQty,
                Amount = (decimal)productionQty * plannedUnitPrice
            },
            OtherExport = new QuantityAmountDto
            {
                Quantity = otherQty,
                Amount = (decimal)otherQty * plannedUnitPrice
            },
            ContractSettlement = new QuantityAmountDto
            {
                Quantity = contractQty,
                Amount = (decimal)contractQty * plannedUnitPrice
            },
            Total = new TotalDto
            {
                Quantity = item.ShippedQuantity,
                Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
            }
        };
    }

    private static double GetIssuedQuantity(AcceptanceReportItem item, IssuedQuantityType type)
        => item.IssuedDetails
            .Where(d => d.Type == type)
            .Sum(d => d.Quantity);

    private static double GetShippedQuantity(AcceptanceReportItem item, ShippedQuantityType type)
        => item.ShippedDetails
            .Where(d => d.Type == type)
            .Sum(d => d.Quantity);

    private static (decimal planned, decimal actual) GetUnitPrices(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        var cost = costs?.FirstOrDefault(c => c.StartMonth <= month && c.EndMonth >= month);
        if (cost == null)
        {
            return (0, 0);
        }

        return ((decimal)cost.Amount, (decimal)cost.ActualAmount);
    }
}
