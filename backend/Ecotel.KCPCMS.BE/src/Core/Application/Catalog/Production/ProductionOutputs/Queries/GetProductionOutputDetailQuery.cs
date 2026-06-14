using Application.Common.Exceptions;
using Application.Common.Repositories;
using Application.Common.UnitOfWork;
using Application.Catalog.Production.LongTermAnchorSeeds;
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

    private readonly IWriteRepository<LongTermAnchorSeed> _seedRepository =
        unitOfWork.GetRepository<LongTermAnchorSeed>();

    public async Task<ProductionOutputDetailResponseDto> Handle(
        GetProductionOutputDetailQuery request, CancellationToken cancellationToken)
    {
        var productionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.ProductionOutputId,
            include: q => q
                .Include(p => p.Department).ThenInclude(d => d.Code)
                .Include(p => p.ProductionOutputProcessGroups)
                    .ThenInclude(g => g.ProductionOutputProducts),
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
                    .ThenInclude(i => i.Part).ThenInclude(p => p.AssignmentCodeMaterials)
                    .ThenInclude(link => link.AssignmentCode).ThenInclude(ac => ac.Code)
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
            var anchorSeedSnapshots = await BuildAnchorSeedSnapshots(acceptanceReport, cancellationToken);

            sectionA = BuildSectionA(acceptanceReport, previousReports, productionOutput, anchorSeedSnapshots);
            sectionB = BuildSectionB(acceptanceReport, previousReports, productionOutput);
            sectionC = BuildSectionC(acceptanceReport, productionOutput);
            sectionD = BuildSectionD(acceptanceReport, productionOutput);
        }

        return new ProductionOutputDetailResponseDto
        {
            ProductionOutputId = productionOutput.Id,
            StartMonth = productionOutput.StartMonth,
            EndMonth = productionOutput.EndMonth,
            DepartmentId = productionOutput.DepartmentId,
            DepartmentCode = productionOutput.Department?.Code?.Value ?? string.Empty,
            DepartmentName = productionOutput.Department?.Name ?? string.Empty,
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
    //    Không có ProductionOrderId       → group theo AssignmentCode.Code
    //
    //  Sub-section 3 (SectionAType=3): SCTX TH2 — chi phí dài kỳ phân bổ
    //    Thuộc kỳ trước + có ProductionOrderId → group theo ProductionOrderId
    //    Còn lại                               → group theo AssignmentCode.Code | "VTK"
    // =========================================================================
    private List<MaterialGroupDto> BuildSectionA(
        AcceptanceReport report,
        List<AcceptanceReport> previousReports,
        ProductionOutput productionOutput,
        List<AnchorSeedSnapshotContext> anchorSeedSnapshots)
    {
        var groups = new Dictionary<string, MaterialGroupDto>();

        var sectionItems = report.AcceptanceReportItems
            .Where(i => i.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
            .ToList();

        // ── Sub-section 1: Vật liệu ──────────────────────────────────────────
        foreach (var item in sectionItems.Where(i => i.IsMaterialItem && i.Material != null))
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
        foreach (var item in sectionItems.Where(IsSctxItem))
        {
            var material = GetSctxMaterial(item)!;
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

            var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
            if (!item.IsLongTermTracking)
            {
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxImmediateExpenseDetail(item, material, plannedPrice, actualPrice));
                continue;
            }

            var th1Logs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId == report.Id)
                .ToList();

            if (th1Logs.Any())
            {
                foreach (var log in th1Logs)
                {
                    AddSctxDetailToGroup(
                        group,
                        item,
                        BuildSctxTh1Detail(item, material, plannedPrice, actualPrice, log));
                }
            }
            else
            {
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxNoLogDetail(item, material, plannedPrice, actualPrice));
            }
        }

        // ── Sub-section 3: SCTX TH2 (từ current report — logs kỳ trước còn tồn) ──
        foreach (var item in sectionItems.Where(IsSctxItem))
        {
            if (!item.IsLongTermTracking)
            {
                continue;
            }

            var material = GetSctxMaterial(item)!;
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

            var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
            AddSctxDetailToGroup(
                group,
                item,
                BuildSctxTh2Detail(item, material, plannedPrice, actualPrice, oldLogs, productionOutput));
        }

        // SCTX TH2 từ previous reports
        foreach (var prevReport in previousReports)
        {
            var prevItems = prevReport.AcceptanceReportItems
                .Where(i => i.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain
                         && IsSctxItem(i)
                         && i.IsLongTermTracking)
                .ToList();

            foreach (var item in prevItems)
            {
                var material = GetSctxMaterial(item)!;
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

                var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxTh2Detail(item, material, plannedPrice, actualPrice, oldLogs, productionOutput));
            }
        }

        foreach (var anchorSeedSnapshot in anchorSeedSnapshots)
        {
            var material = anchorSeedSnapshot.Material;
            var snapshot = anchorSeedSnapshot.Snapshot;
            var (groupKey, groupCode, groupName) = ResolveAnchorSeedTh2GroupKey("A3", material);
            var group = GetOrAddGroup(groups, groupKey, new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = MatTypeLabel.Sctx,
                SectionAType = SecAType.SctxTh2,
                Materials = new(),
                SubGroups = new()
            });

            var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
            group.Materials.Add(BuildAnchorSeedTh2Detail(snapshot, material, plannedPrice, actualPrice));
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
    //    Group theo Lệnh sản xuất / "NO_ORDER"
    //    và subgroup theo Nhóm vật tư, tài sản đã chọn trong BBNT
    //
    //  AdditionalCost = Maintain (SCTX):
    //    Group theo Lệnh sản xuất / "NO_ORDER"
    //    và subgroup theo Nhóm vật tư, tài sản đã chọn trong BBNT
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
                     && i.IsMaterialItem && i.Material != null))
        {
            var (groupKey, groupCode, groupName) = ResolveAdditionalCostOrderGroupKey("BM", item);
            var productionOrderId = item.AdditionalCostProductionOrderId;

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
            AddAdditionalMaterialDetailToGroup(
                group,
                item,
                BuildVatLieuDetail(item, item.Material, plannedPrice, actualPrice, true));
        }

        // ── SCTX TH1 ─────────────────────────────────────────────────────────
        foreach (var item in sectionItems
            .Where(i => i.AdditionalCost == AdditionalCost.Maintain
                     && IsSctxItem(i)))
        {
            var material = GetSctxMaterial(item)!;
            var (groupKey, groupCode, groupName) = ResolveAdditionalCostOrderGroupKey("BS", item);
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

            var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
            var th1Logs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId == report.Id)
                .ToList();
            if (th1Logs.Any())
            {
                foreach (var log in th1Logs)
                {
                    AddSctxDetailToGroup(
                        group,
                        item,
                        BuildSctxTh1Detail(item, material, plannedPrice, actualPrice, log, true),
                        true,
                        true);
                }
            }
            else
            {
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxNoLogDetail(item, material, plannedPrice, actualPrice, true),
                    true,
                    true);
            }

            var oldLogs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId != report.Id && l.RemainingTime > 0)
                .OrderByDescending(l => l.PeriodEndMonth)
                .ToList();
            if (oldLogs.Any())
            {
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxTh2Detail(item, material, plannedPrice, actualPrice, oldLogs, productionOutput, true),
                    true,
                    true);
            }
        }

        // SCTX TH2 từ previous reports
        foreach (var prevReport in previousReports)
        {
            var prevItems = prevReport.AcceptanceReportItems
                .Where(i => i.AdditionalCost == AdditionalCost.Maintain
                         && IsSctxItem(i))
                .ToList();

            foreach (var item in prevItems)
            {
                var material = GetSctxMaterial(item)!;
                var (groupKey, groupCode, groupName) = ResolveAdditionalCostOrderGroupKey("BS", item);
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

                var (plannedPrice, actualPrice) = GetUnitPrices(material.Costs, productionOutput.StartMonth);
                AddSctxDetailToGroup(
                    group,
                    item,
                    BuildSctxTh2Detail(item, material, plannedPrice, actualPrice, oldLogs, productionOutput, true),
                    true,
                    true);
            }
        }

        // ── OtherMaterial ─────────────────────────────────────────────────────
        foreach (var item in sectionItems
            .Where(i => i.AdditionalCost == AdditionalCost.SafeAndWelfare
                     && i.IsMaterialItem && i.Material != null))
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
                     && i.IsMaterialItem && i.Material != null)
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
            .Where(i => i.Asset != Asset.None && i.IsMaterialItem && i.Material != null)
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
        var material = GetSctxMaterial(item);
        if (material?.LegacyPartType == PartType.OtherPart)
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

        var assignmentCode = ResolveAssignmentCodeForGrouping(item, useAdditionalCostReference);
        var code = assignmentCode?.Code?.Value ?? "VTK";
        var name = assignmentCode?.Name ?? "Vật tư khác";
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

        var assignmentCode = ResolveAssignmentCodeForGrouping(item, useAdditionalCostReference);
        var code = assignmentCode?.Code?.Value ?? "VTK";
        var name = assignmentCode?.Name ?? "Vật tư khác";
        return ($"{prefix}_EQ_{code}", code, name);
    }

    private static (string key, string code, string name) ResolveAdditionalCostOrderGroupKey(
        string prefix,
        AcceptanceReportItem item)
    {
        if (item.AdditionalCostProductionOrderId.HasValue)
        {
            var id = item.AdditionalCostProductionOrderId.Value.ToString();
            return ($"{prefix}_PO_{id}", id, id);
        }

        return ($"{prefix}_NO_ORDER", "NO_ORDER", "Không theo lệnh sản xuất");
    }

    private static (string key, string code, string name) ResolveAnchorSeedTh2GroupKey(
        string prefix,
        SctxMaterialContext material)
    {
        var assignmentCode = material.AssignmentCodes?.FirstOrDefault();
        var code = assignmentCode?.Code?.Value ?? "VTK";
        var name = assignmentCode?.Name ?? "Vật tư khác";
        return ($"{prefix}_EQ_{code}", code, name);
    }

    private static void AddSctxDetailToGroup(
        MaterialGroupDto group,
        AcceptanceReportItem item,
        MaterialDetailDto detail,
        bool useAdditionalCostReference = false,
        bool forceAssignmentSubGroup = false)
    {
        var productionOrderId = useAdditionalCostReference
            ? item.AdditionalCostProductionOrderId
            : item.ProductionOrderId;

        if (!productionOrderId.HasValue && !forceAssignmentSubGroup)
        {
            group.Materials.Add(detail);
            return;
        }

        var assignmentCode = ResolveAssignmentCodeForGrouping(item, useAdditionalCostReference);
        if (assignmentCode == null)
        {
            if (forceAssignmentSubGroup)
            {
                var defaultSubGroup = group.SubGroups.FirstOrDefault(s => s.SubGroupCode == string.Empty);
                if (defaultSubGroup == null)
                {
                    defaultSubGroup = new SubGroupDto
                    {
                        SubGroupCode = string.Empty,
                        SubGroupName = "Không thuộc nhóm vật tư, tài sản",
                        Materials = new()
                    };
                    group.SubGroups.Add(defaultSubGroup);
                }

                defaultSubGroup.Materials.Add(detail);
                return;
            }

            group.Materials.Add(detail);
            return;
        }

        var subGroupCode = assignmentCode.Code?.Value ?? assignmentCode.Id.ToString();
        var subGroupName = assignmentCode.Name ?? subGroupCode;
        var subGroup = group.SubGroups.FirstOrDefault(s => s.SubGroupCode == subGroupCode);

        if (subGroup == null)
        {
            subGroup = new SubGroupDto
            {
                SubGroupCode = subGroupCode,
                SubGroupName = subGroupName,
                Materials = new()
            };
            group.SubGroups.Add(subGroup);
        }

        subGroup.Materials.Add(detail);
    }

    private static void AddAdditionalMaterialDetailToGroup(
        MaterialGroupDto group,
        AcceptanceReportItem item,
        MaterialDetailDto detail)
    {
        var assignmentCode = ResolveAdditionalMaterialAssignmentCode(item);
        var subGroupCode = assignmentCode?.Code?.Value ?? string.Empty;
        var subGroupName = assignmentCode?.Name ?? "Không thuộc nhóm vật tư, tài sản";
        var subGroup = group.SubGroups.FirstOrDefault(s => s.SubGroupCode == subGroupCode);

        if (subGroup == null)
        {
            subGroup = new SubGroupDto
            {
                SubGroupCode = subGroupCode,
                SubGroupName = subGroupName,
                Materials = new()
            };
            group.SubGroups.Add(subGroup);
        }

        subGroup.Materials.Add(detail);
    }

    private static AssignmentCode? ResolveAssignmentCodeForGrouping(
        AcceptanceReportItem item,
        bool useAdditionalCostReference)
    {
        var material = GetSctxMaterial(item);

        if (!useAdditionalCostReference)
        {
            return item.Equipment ?? material?.AssignmentCodes?.FirstOrDefault();
        }

        if (item.AdditionalCostAssignmentCodeId.HasValue)
        {
            return material?.AssignmentCodes?
                .FirstOrDefault(ac => ac.Id == item.AdditionalCostAssignmentCodeId.Value);
        }

        return material?.AssignmentCodes?.FirstOrDefault();
    }

    private static AssignmentCode? ResolveAdditionalMaterialAssignmentCode(AcceptanceReportItem item)
    {
        if (!item.AdditionalCostAssignmentCodeId.HasValue)
        {
            return null;
        }

        if (item.Material?.AssignmentCode?.Id == item.AdditionalCostAssignmentCodeId.Value)
        {
            return item.Material.AssignmentCode;
        }

        return null;
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
        SctxMaterialContext material,
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
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasureName,
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

    private static MaterialDetailDto BuildSctxNoLogDetail(
        AcceptanceReportItem item,
        SctxMaterialContext material,
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
            MaterialCode = material.Code,
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasureName,
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

    private static MaterialDetailDto BuildSctxImmediateExpenseDetail(
        AcceptanceReportItem item,
        SctxMaterialContext material,
        decimal plannedPrice,
        decimal actualPrice)
    {
        if (item.ShippedQuantity >= item.IssuedQuantity)
        {
            return BuildSctxNoLogDetail(item, material, plannedPrice, actualPrice);
        }

        var issued = BuildIssuedInPeriod(item, plannedPrice, actualPrice);
        var exportedAmount = (decimal)item.IssuedQuantity * plannedPrice;
        var hasProductionOrder = item.ProductionOrderId.HasValue;

        return new MaterialDetailDto
        {
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasureName,
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            IssuedInPeriod = issued,
            ExportedInPeriod = new ExportedInPeriodDto
            {
                ExportedToProduction = new ExportedToProductionDto
                {
                    Quantity = item.IssuedQuantity,
                    Amount = exportedAmount
                },
                OtherExport = new QuantityAmountDto(),
                ContractSettlement = new QuantityAmountDto(),
                Total = new TotalDto
                {
                    Quantity = item.IssuedQuantity,
                    Amount = exportedAmount
                }
            },
            EndingInventory = new EndingInventoryDto
            {
                RemainingAtSite = !hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = 0, Amount = 0 }
                    : null,
                RemainingByOrder = hasProductionOrder
                    ? new InventoryQuantityDto { Quantity = 0, Amount = 0 }
                    : null,
                Total = new TotalDto { Quantity = 0, Amount = 0 }
            }
        };
    }

    private static MaterialDetailDto BuildSctxTh2Detail(
        AcceptanceReportItem item, SctxMaterialContext material, decimal plannedPrice, decimal actualPrice,
        List<AcceptanceReportItemLog> oldLogs, ProductionOutput productionOutput,
        bool useAdditionalCostReference = false)
    {
        var latestLog = oldLogs.First();
        var pendingStart = latestLog.PendingValueEndPeriod;
        var plannedOutput = productionOutput.ProductionOutputProcessGroups
            .Sum(x => x.PlanProductionMeters);

        decimal accountedThisPeriod = 0;
        if (latestLog.UsageTime > 0 && productionOutput.StandardProductionMeters > 0)
        {
            var valueByStandard = (pendingStart / (decimal)latestLog.UsageTime)
                * ((decimal)plannedOutput
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
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasureName,
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            BeginningInventory = beginningInventory,
            IssuedInPeriod = issued,
            ExportedInPeriod = exported,
            EndingInventory = endingInventory
        };
    }

    private static MaterialDetailDto BuildAnchorSeedTh2Detail(
        LongTermAnchorSeedTrackingHelper.TrackingSnapshot snapshot,
        SctxMaterialContext material,
        decimal plannedPrice,
        decimal actualPrice)
    {
        return new MaterialDetailDto
        {
            MaterialId = material.Id,
            MaterialCode = material.Code,
            MaterialName = material.Name,
            UnitOfMeasureName = material.UnitOfMeasureName,
            PlannedUnitPrice = plannedPrice,
            ActualUnitPrice = actualPrice,
            BeginningInventory = new BeginningInventoryDto
            {
                RemainingAtSite = new InventoryQuantityDto
                {
                    Quantity = 0,
                    Amount = snapshot.PendingValueStartPeriod
                },
                PendingValue = snapshot.PendingValueStartPeriod,
                Total = new TotalDto
                {
                    Quantity = 0,
                    Amount = snapshot.PendingValueStartPeriod
                }
            },
            IssuedInPeriod = new IssuedInPeriodDto
            {
                Received = new ReceivedSuppliesDto { Quantity = 0, PlannedAmount = 0, ActualAmount = 0 },
                BorrowedNoVoucher = new QuantityAmountDto(),
                ReturnPreviousMonthVoucher = new QuantityAmountDto(),
                OtherReceipt = new QuantityAmountDto(),
                Total = new TotalDto()
            },
            ExportedInPeriod = new ExportedInPeriodDto
            {
                ExportedToProduction = new ExportedToProductionDto(),
                OtherExport = new QuantityAmountDto(),
                ContractSettlement = new QuantityAmountDto(),
                LongTermExpense = new LongTermExpenseDto
                {
                    Amount = snapshot.AccountedValueThisPeriod
                },
                Total = new TotalDto
                {
                    Quantity = 0,
                    Amount = snapshot.AccountedValueThisPeriod
                }
            },
            EndingInventory = new EndingInventoryDto
            {
                RemainingAtSite = new InventoryQuantityDto
                {
                    Quantity = 0,
                    Amount = snapshot.PendingValueEndPeriod
                },
                PendingValue = snapshot.PendingValueEndPeriod,
                Total = new TotalDto
                {
                    Quantity = 0,
                    Amount = snapshot.PendingValueEndPeriod
                }
            }
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
                        .ThenInclude(i => i.Equipment).ThenInclude(e => e.Code)
                    .Include(a => a.AcceptanceReportItems)
                        .ThenInclude(i => i.Part).ThenInclude(p => p.AssignmentCodeMaterials)
                            .ThenInclude(link => link.AssignmentCode).ThenInclude(ac => ac.Code)
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

    private async Task<List<AnchorSeedSnapshotContext>> BuildAnchorSeedSnapshots(
        AcceptanceReport acceptanceReport,
        CancellationToken cancellationToken)
    {
        var departmentId = acceptanceReport.ProductionOutput?.DepartmentId;
        if (!departmentId.HasValue)
        {
            return [];
        }

        var seed = await _seedRepository.GetFirstOrDefaultAsync(
            predicate: s => s.DepartmentId == departmentId.Value,
            include: q => q
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.Code)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.Costs)
                .Include(s => s.Items)
                    .ThenInclude(i => i.Part)
                        .ThenInclude(p => p.AssignmentCodeMaterials)
                            .ThenInclude(link => link.AssignmentCode)
                                .ThenInclude(ac => ac.Code)
                .Include(s => s.Items)
                    .ThenInclude(i => i.ProcessGroup)
                        .ThenInclude(pg => pg.Code)
                .Include(s => s.ProcessGroupMetrics),
            disableTracking: true);

        if (seed == null || !seed.Items.Any())
        {
            return [];
        }

        var processGroupMetrics = seed.ProcessGroupMetrics
            .GroupBy(x => x.ProcessGroupId)
            .ToDictionary(
                x => x.Key,
                x => (
                    PlannedOutput: x.First().PlannedOutput,
                    StandardOutput: x.First().StandardOutput));

        var reports = await _acceptanceReportRepository.GetAllAsync(
            predicate: a => a.ProductionOutput.DepartmentId == departmentId.Value,
            include: q => q
                .Include(a => a.ProductionOutput)
                    .ThenInclude(p => p.ProductionOutputProcessGroups)
                        .ThenInclude(pg => pg.ProductionOutputProducts),
            disableTracking: true);

        var orderedReports = reports
            .Where(a => a.ProductionOutput != null)
            .OrderBy(a => a.ProductionOutput!.StartMonth)
            .Select(a => new LongTermAnchorSeedTrackingHelper.ReportContext(
                a.Id,
                a.ProductionOutput!.StartMonth,
                a.ProductionOutput.ProductionMeters,
                a.ProductionOutput.StandardProductionMeters,
                BuildOutputByProcessGroup(a.ProductionOutput)))
            .ToList();

        var snapshots = LongTermAnchorSeedTrackingHelper.BuildSnapshots(
            seed.Items,
            processGroupMetrics,
            orderedReports,
            acceptanceReport.Id);

        var itemById = seed.Items.ToDictionary(x => x.Id);

        return snapshots
            .Where(snapshot => itemById.ContainsKey(snapshot.SeedItemId))
            .Select(snapshot => new AnchorSeedSnapshotContext(
                snapshot,
                BuildSctxMaterial(itemById[snapshot.SeedItemId].Part)))
            .ToList();
    }

    private static Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)> BuildOutputByProcessGroup(
        ProductionOutput productionOutput)
    {
        var result = new Dictionary<Guid, (double ActualOutput, double PlannedOutput, double StandardOutput)>();

        foreach (var processGroup in productionOutput.ProductionOutputProcessGroups)
        {
            result[processGroup.ProcessGroupId] = (
                processGroup.ProductionMeters,
                processGroup.PlanProductionMeters,
                processGroup.StandardProductionMeters);
        }

        return result;
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

    private static bool IsSctxItem(AcceptanceReportItem item)
        => item.IsTrackedSctxItem && GetSctxMaterial(item) != null;

    private static SctxMaterialContext? GetSctxMaterial(AcceptanceReportItem item)
        => !item.IsTrackedSctxItem || item.Part == null ? null : BuildSctxMaterial(item.Part);

    private static SctxMaterialContext BuildSctxMaterial(Material part)
        => new(
            part.Id,
            part.Code?.Value ?? string.Empty,
            part.Name,
            part.UnitOfMeasure?.Name ?? string.Empty,
            part.Costs,
            part.MaterialType == MaterialType.MaterialOutContract ? PartType.OtherPart : PartType.Part,
            part.AssignmentCodeMaterials,
            part.AssignmentCodeMaterials
                .Select(link => link.AssignmentCode)
                .Where(assignmentCode => assignmentCode != null)
                .ToList()!);

    private static (decimal planned, decimal actual) GetUnitPrices(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        var cost = costs?.FirstOrDefault(c => c.StartMonth <= month && c.EndMonth >= month);
        if (cost == null)
        {
            return (0, 0);
        }

        return ((decimal)cost.Amount, (decimal)cost.ActualAmount);
    }

    private sealed record SctxMaterialContext(
        Guid Id,
        string Code,
        string Name,
        string UnitOfMeasureName,
        IReadOnlyCollection<Cost> Costs,
        PartType LegacyPartType,
        IReadOnlyCollection<AssignmentCodeMaterial> AssignmentCodeMaterials,
        IReadOnlyCollection<AssignmentCode> AssignmentCodes);

    private sealed record AnchorSeedSnapshotContext(
        LongTermAnchorSeedTrackingHelper.TrackingSnapshot Snapshot,
        SctxMaterialContext Material);
}

