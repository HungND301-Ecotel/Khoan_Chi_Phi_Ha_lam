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

public class GetProductionOutputDetailQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetProductionOutputDetailQuery, ProductionOutputDetailResponseDto>
{
    private readonly IWriteRepository<ProductionOutput> _productionOutputRepository = unitOfWork.GetRepository<ProductionOutput>();
    private readonly IWriteRepository<AcceptanceReport> _acceptanceReportRepository = unitOfWork.GetRepository<AcceptanceReport>();

    public async Task<ProductionOutputDetailResponseDto> Handle(GetProductionOutputDetailQuery request, CancellationToken cancellationToken)
    {
        // Get ProductionOutput
        var productionOutput = await _productionOutputRepository.GetFirstOrDefaultAsync(
            predicate: p => p.Id == request.ProductionOutputId,
            disableTracking: true);

        if (productionOutput == null)
        {
            throw new NotFoundException(CustomResponseMessage.EntityNotFound);
        }

        // Get AcceptanceReport for this ProductionOutput
        var acceptanceReport = await _acceptanceReportRepository.GetFirstOrDefaultAsync(
            predicate: a => a.ProductionOutputId == request.ProductionOutputId,
            include: q => q
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.AssignmentCode)
                            .ThenInclude(ac => ac.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Costs)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.MaintainUnitPriceEquipment)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.MaintainUnitPriceEquipment)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.Equipment)
                                .ThenInclude(e => e.Code)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.MaintainUnitPriceEquipment)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.UnitOfMeasure)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.MaintainUnitPriceEquipment)
                        .ThenInclude(m => m.Part)
                            .ThenInclude(p => p.Costs)
                .Include(a => a.AcceptanceReportItems)
                    .ThenInclude(i => i.AcceptanceReportItemLogs)
                .Include(a => a.ProductionOutput),
            disableTracking: true);

        var items = new List<ProductionOutputDetailItemDto>();

        if (acceptanceReport != null)
        {
            // Get all AcceptanceReports from previous months to get TH2 data
            var allPreviousAcceptanceReports = new List<AcceptanceReport>();
            try
            {
                // Lấy tất cả AcceptanceReports để filter trong memory
                var allAcceptanceReports = await _acceptanceReportRepository.GetAllAsync(
                    include: q => q
                        .Include(a => a.ProductionOutput)
                        .Include(a => a.AcceptanceReportItems)
                            .ThenInclude(i => i.MaintainUnitPriceEquipment)
                                .ThenInclude(m => m.Part)
                                    .ThenInclude(p => p.Code)
                        .Include(a => a.AcceptanceReportItems)
                            .ThenInclude(i => i.MaintainUnitPriceEquipment)
                                .ThenInclude(m => m.Part)
                                    .ThenInclude(p => p.Equipment)
                                        .ThenInclude(e => e.Code)
                        .Include(a => a.AcceptanceReportItems)
                            .ThenInclude(i => i.MaintainUnitPriceEquipment)
                                .ThenInclude(m => m.Part)
                                    .ThenInclude(p => p.UnitOfMeasure)
                        .Include(a => a.AcceptanceReportItems)
                            .ThenInclude(i => i.MaintainUnitPriceEquipment)
                                .ThenInclude(m => m.Part)
                                    .ThenInclude(p => p.Costs)
                        .Include(a => a.AcceptanceReportItems)
                            .ThenInclude(i => i.AcceptanceReportItemLogs),
                    disableTracking: true);

                allPreviousAcceptanceReports = allAcceptanceReports
                    .Where(a => a.ProductionOutput != null && a.ProductionOutput.StartMonth < productionOutput.StartMonth)
                    .OrderByDescending(a => a.ProductionOutput.StartMonth)
                    .ToList();
            }
            catch
            {
                // If there's any error, continue with empty list
                allPreviousAcceptanceReports = new List<AcceptanceReport>();
            }

            // Category I: Materials in Contract Revenue
            items.Add(await BuildMaterialsInContractRevenueCategory(
                acceptanceReport, allPreviousAcceptanceReports, productionOutput, cancellationToken));

            // Category II: Additional Cost
            items.Add(await BuildAdditionalCostCategory(
                acceptanceReport, allPreviousAcceptanceReports, productionOutput, cancellationToken));

            // Category III: Quota-Based Material
            items.Add(await BuildQuotaBasedMaterialCategory(
                acceptanceReport, allPreviousAcceptanceReports, productionOutput, cancellationToken));

            // Category IV: Asset
            items.Add(await BuildAssetCategory(
                acceptanceReport, allPreviousAcceptanceReports, productionOutput, cancellationToken));
        }

        return new ProductionOutputDetailResponseDto
        {
            ProductionOutputId = productionOutput.Id,
            StartMonth = productionOutput.StartMonth,
            EndMonth = productionOutput.EndMonth,
            ProductionMeters = productionOutput.ProductionMeters,
            StandardProductionMeters = productionOutput.StandardProductionMeters,
            Items = items
        };
    }

    private async Task<ProductionOutputDetailItemDto> BuildMaterialsInContractRevenueCategory(
        AcceptanceReport acceptanceReport, List<AcceptanceReport> previousAcceptanceReports, ProductionOutput productionOutput, CancellationToken cancellationToken)
    {
        var categoryItems = acceptanceReport.AcceptanceReportItems
            .Where(i => i.MaterialsIncludedInContractRevenue != MaterialsIncludedInContractRevenue.None)
            .ToList();

        var materialGroups = new Dictionary<string, MaterialGroupDto>();

        // Process Materials từ current month (TH1)
        var materials = categoryItems.Where(i => i.MaterialId.HasValue).ToList();
        foreach (var item in materials)
        {
            if (item.Material == null)
            {
                continue;
            }

            var groupCode = item.Material.AssignmentCode?.Code?.Value ?? "VTK";
            var groupName = item.Material.AssignmentCode?.Name ?? "Vật tư khác";

            if (!materialGroups.ContainsKey(groupCode))
            {
                materialGroups[groupCode] = new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = "Vật liệu",
                    Materials = new()
                };
            }

            var plannedUnitPrice = GetUnitPrice(item.Material.Costs, productionOutput.StartMonth);
            var materialDetail = new MaterialDetailDto
            {
                MaterialId = item.Material.Id,
                MaterialCode = item.Material.Code?.Value ?? "",
                MaterialName = item.Material.Name,
                UnitOfMeasureName = item.Material.UnitOfMeasure?.Name ?? "",
                PlannedUnitPrice = plannedUnitPrice,
                ActualUnitPrice = 0,
                IssuedInPeriod = new IssuedInPeriodDto
                {
                    Received = new ReceivedSuppliesDto
                    {
                        Quantity = item.IssuedQuantity,
                        PlannedAmount = (decimal)item.IssuedQuantity * plannedUnitPrice,
                        ActualAmount = 0
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.IssuedQuantity,
                        Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
                    }
                },
                ExportedInPeriod = new ExportedInPeriodDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    }
                }
            };

            materialGroups[groupCode].Materials.Add(materialDetail);
        }

        // Process SCTX (Parts) từ current month (TH1)
        var sctxItems = categoryItems.Where(i => i.MaintainUnitPriceEquipmentId.HasValue).ToList();
        foreach (var item in sctxItems)
        {
            if (item.MaintainUnitPriceEquipment?.Part == null)
            {
                continue;
            }

            var part = item.MaintainUnitPriceEquipment.Part;
            var groupCode = part.Equipment?.Code?.Value ?? "VTK";
            var groupName = part.Equipment?.Name ?? "Vật tư khác";

            if (!materialGroups.ContainsKey(groupCode))
            {
                materialGroups[groupCode] = new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = "SCTX",
                    Materials = new()
                };
            }

            var plannedUnitPrice = GetUnitPrice(part.Costs, productionOutput.StartMonth);

            // TH1: Logs từ current AcceptanceReport
            var th1Logs = item.AcceptanceReportItemLogs
                .Where(l => l.AcceptanceReportId == acceptanceReport.Id)
                .ToList();

            // Process TH1: Item mới lĩnh - CHỈ có IssuedInPeriod + ExportedInPeriod, KHÔNG có BeginningInventory
            foreach (var log in th1Logs)
            {
                var materialDetail = new MaterialDetailDto
                {
                    MaterialId = part.Id,
                    MaterialCode = part.Code?.Value ?? "",
                    MaterialName = part.Name,
                    UnitOfMeasureName = part.UnitOfMeasure?.Name ?? "",
                    PlannedUnitPrice = plannedUnitPrice,
                    ActualUnitPrice = 0,
                    IssuedInPeriod = new IssuedInPeriodDto
                    {
                        Received = new ReceivedSuppliesDto
                        {
                            Quantity = log.IssuedQuantity,
                            PlannedAmount = (decimal)log.IssuedQuantity * plannedUnitPrice,
                            ActualAmount = 0
                        },
                        Total = new TotalDto
                        {
                            Quantity = log.IssuedQuantity,
                            Amount = (decimal)log.IssuedQuantity * plannedUnitPrice
                        }
                    },
                    ExportedInPeriod = new ExportedInPeriodDto
                    {
                        ExportedToProduction = new ExportedToProductionDto
                        {
                            Quantity = item.ShippedQuantity,
                            Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                        },
                        LongTermExpense = new LongTermExpenseDto
                        {
                            Amount = log.AccountedValueThisPeriod
                        },
                        Total = new TotalDto
                        {
                            Quantity = 0,
                            Amount = log.AccountedValueThisPeriod
                        }
                    }
                    // NO BeginningInventory, NO EndingInventory
                };

                materialGroups[groupCode].Materials.Add(materialDetail);
            }

            // TH2: Item cũ từ kỳ trước - Có BeginningInventory + EndingInventory
            // Lấy logs cũ với RemainingTime >= 0 (exclude logs từ current AcceptanceReport)
            var previousLogs = item.AcceptanceReportItemLogs
                .Where(l => l.RemainingTime >= 0)
                .OrderByDescending(l => l.PeriodEndMonth)
                .ToList();

            // Loại bỏ logs từ current AcceptanceReport để chỉ lấy logs từ kỳ trước
            var oldLogs = previousLogs
                .Where(l => l.AcceptanceReportId != acceptanceReport.Id)
                .ToList();

            if (oldLogs.Any())
            {
                ProcessOldLog(oldLogs, item, part, groupCode, materialGroups, plannedUnitPrice, productionOutput);
            }
        }

        // Process items từ các tháng trước (TH2) - lấy items từ previous AcceptanceReports
        foreach (var prevAcceptanceReport in previousAcceptanceReports)
        {
            var prevCategoryItems = prevAcceptanceReport.AcceptanceReportItems
                .Where(i => i.MaterialsIncludedInContractRevenue == MaterialsIncludedInContractRevenue.Maintain)
                .ToList();

            var prevSctxItems = prevCategoryItems.Where(i => i.MaintainUnitPriceEquipmentId.HasValue).ToList();
            foreach (var item in prevSctxItems)
            {
                if (item.MaintainUnitPriceEquipment?.Part == null)
                {
                    continue;
                }

                var part = item.MaintainUnitPriceEquipment.Part;
                var groupCode = part.Equipment?.Code?.Value ?? "VTK";
                var groupName = part.Equipment?.Name ?? "Vật tư khác";

                if (!materialGroups.ContainsKey(groupCode))
                {
                    materialGroups[groupCode] = new MaterialGroupDto
                    {
                        GroupCode = groupCode,
                        GroupName = groupName,
                        MaterialType = "SCTX",
                        Materials = new()
                    };
                }

                var plannedUnitPrice = GetUnitPrice(part.Costs, productionOutput.StartMonth);

                // Lấy logs từ previous AcceptanceReport
                var previousLogs = item.AcceptanceReportItemLogs
                    .Where(l => l.RemainingTime >= 0)
                    .OrderByDescending(l => l.PeriodEndMonth)
                    .ToList();

                if (previousLogs.Any())
                {
                    ProcessOldLog(previousLogs, item, part, groupCode, materialGroups, plannedUnitPrice, productionOutput);
                }
            }
        }

        return new ProductionOutputDetailItemDto
        {
            CategoryType = 1,
            CategoryName = "Vật tư tính vào doanh thu khoán",
            MaterialGroups = materialGroups.Values.OrderBy(g => g.GroupCode).ToList()
        };
    }

    private void ProcessOldLog(List<AcceptanceReportItemLog> oldLogs, AcceptanceReportItem item,
        Part part, string groupCode, Dictionary<string, MaterialGroupDto> materialGroups,
        decimal plannedUnitPrice, ProductionOutput productionOutput)
    {
        var latestLog = oldLogs.First();
        var totalAllocatedTime = oldLogs.Sum(l => l.AllocationRatio);

        var usageTime = latestLog.UsageTime;
        var remainingTime = usageTime - totalAllocatedTime;

        if (remainingTime > 0)
        {
            var pendingValueStart = latestLog.PendingValueEndPeriod;
            var totalValueToAccount = pendingValueStart;

            var actualOutput = productionOutput.ProductionMeters;
            var standardOutput = productionOutput.StandardProductionMeters;

            decimal valueByStandard = 0;
            if (usageTime > 0 && standardOutput > 0)
            {
                valueByStandard = (totalValueToAccount / (decimal)usageTime)
                                  * ((decimal)actualOutput / (decimal)standardOutput);
            }

            var allocationRatio = latestLog.AllocationRatio;
            var accountedValueThisPeriod = valueByStandard * (decimal)allocationRatio;
            var pendingValueEnd = totalValueToAccount - accountedValueThisPeriod;
            var endingQuantity = item.IssuedQuantity - item.ShippedQuantity;

            var materialDetail = new MaterialDetailDto
            {
                MaterialId = part.Id,
                MaterialCode = part.Code?.Value ?? "",
                MaterialName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name ?? "",
                PlannedUnitPrice = plannedUnitPrice,
                ActualUnitPrice = 0,
                BeginningInventory = new BeginningInventoryDto
                {
                    PendingValue = pendingValueStart,
                    Total = new TotalDto
                    {
                        Quantity = 0,
                        Amount = pendingValueStart
                    }
                },
                IssuedInPeriod = new IssuedInPeriodDto
                {
                    Received = new ReceivedSuppliesDto
                    {
                        Quantity = 0,
                        PlannedAmount = 0,
                        ActualAmount = 0
                    },
                    Total = new TotalDto
                    {
                        Quantity = 0,
                        Amount = 0
                    }
                },
                ExportedInPeriod = new ExportedInPeriodDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    },
                    LongTermExpense = new LongTermExpenseDto
                    {
                        Amount = accountedValueThisPeriod
                    },
                    Total = new TotalDto
                    {
                        Quantity = 0,
                        Amount = accountedValueThisPeriod
                    }
                },
                EndingInventory = new EndingInventoryDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = endingQuantity,
                        Amount = (decimal)endingQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = endingQuantity,
                        Amount = pendingValueEnd
                    }
                }
            };

            materialGroups[groupCode].Materials.Add(materialDetail);
        }
    }

    private async Task<ProductionOutputDetailItemDto> BuildAdditionalCostCategory(
        AcceptanceReport acceptanceReport, List<AcceptanceReport> previousAcceptanceReports, ProductionOutput productionOutput, CancellationToken cancellationToken)
    {
        var categoryItems = acceptanceReport.AcceptanceReportItems
            .Where(i => i.AdditionalCost != AdditionalCost.None)
            .ToList();

        var materialGroups = new Dictionary<string, MaterialGroupDto>();

        // Process Materials
        var materials = categoryItems.Where(i => i.MaterialId.HasValue).ToList();
        foreach (var item in materials)
        {
            if (item.Material == null)
            {
                continue;
            }

            var groupCode = "ADDITIONAL_MATERIAL";
            var groupName = "Bổ sung chi phí";

            if (!materialGroups.ContainsKey(groupCode))
            {
                materialGroups[groupCode] = new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = "Vật liệu",
                    Materials = new()
                };
            }

            var plannedUnitPrice = GetUnitPrice(item.Material.Costs, productionOutput.StartMonth);
            var materialDetail = new MaterialDetailDto
            {
                MaterialId = item.Material.Id,
                MaterialCode = item.Material.Code?.Value ?? "",
                MaterialName = item.Material.Name,
                UnitOfMeasureName = item.Material.UnitOfMeasure?.Name ?? "",
                PlannedUnitPrice = plannedUnitPrice,
                ActualUnitPrice = 0,
                IssuedInPeriod = new IssuedInPeriodDto
                {
                    Received = new ReceivedSuppliesDto
                    {
                        Quantity = item.IssuedQuantity,
                        PlannedAmount = (decimal)item.IssuedQuantity * plannedUnitPrice,
                        ActualAmount = 0
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.IssuedQuantity,
                        Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
                    }
                },
                ExportedInPeriod = new ExportedInPeriodDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    }
                }
            };

            materialGroups[groupCode].Materials.Add(materialDetail);
        }

        // Process SCTX
        var sctxItems = categoryItems.Where(i => i.MaintainUnitPriceEquipmentId.HasValue).ToList();
        foreach (var item in sctxItems)
        {
            if (item.MaintainUnitPriceEquipment?.Part == null)
            {
                continue;
            }

            var part = item.MaintainUnitPriceEquipment.Part;
            var groupCode = "ADDITIONAL_SCTX";
            var groupName = "Bổ sung chi phí";

            if (!materialGroups.ContainsKey(groupCode))
            {
                materialGroups[groupCode] = new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = "SCTX",
                    Materials = new()
                };
            }

            var plannedUnitPrice = GetUnitPrice(part.Costs, productionOutput.StartMonth);
            var materialDetail = new MaterialDetailDto
            {
                MaterialId = part.Id,
                MaterialCode = part.Code?.Value ?? "",
                MaterialName = part.Name,
                UnitOfMeasureName = part.UnitOfMeasure?.Name ?? "",
                PlannedUnitPrice = plannedUnitPrice,
                ActualUnitPrice = 0,
                IssuedInPeriod = new IssuedInPeriodDto
                {
                    Received = new ReceivedSuppliesDto
                    {
                        Quantity = item.IssuedQuantity,
                        PlannedAmount = (decimal)item.IssuedQuantity * plannedUnitPrice,
                        ActualAmount = 0
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.IssuedQuantity,
                        Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
                    }
                },
                ExportedInPeriod = new ExportedInPeriodDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    }
                }
            };

            materialGroups[groupCode].Materials.Add(materialDetail);
        }

        return new ProductionOutputDetailItemDto
        {
            CategoryType = 2,
            CategoryName = "Bổ sung chi phí",
            MaterialGroups = materialGroups.Values.OrderBy(g => g.GroupCode).ToList()
        };
    }

    private async Task<ProductionOutputDetailItemDto> BuildQuotaBasedMaterialCategory(
        AcceptanceReport acceptanceReport, List<AcceptanceReport> previousAcceptanceReports, ProductionOutput productionOutput, CancellationToken cancellationToken)
    {
        var categoryItems = acceptanceReport.AcceptanceReportItems
            .Where(i => i.QuotaBasedMaterial != QuotaBasedMaterial.None)
            .ToList();

        var materialGroups = new Dictionary<string, MaterialGroupDto>();

        foreach (var item in categoryItems)
        {
            if (item.Material == null)
            {
                continue;
            }

            // Xác định groupCode và cấu trúc dựa trên QuotaBasedMaterial
            string groupCode;
            string groupName;
            string materialTypeName;
            string? subGroupCode = null;
            string? subGroupName = null;
            string? state = null;
            bool hasSubGroups = false;

            switch (item.QuotaBasedMaterial)
            {
                case QuotaBasedMaterial.MineSupport:
                    groupCode = "MineSupport";
                    groupName = "Vì chống lò";
                    materialTypeName = "Vì chống lò";
                    hasSubGroups = true;

                    if (item.QuotaBasedMaterialType == QuotaBasedMaterialType.New)
                    {
                        subGroupCode = "New";
                        subGroupName = "Lĩnh mới";
                        state = "Lĩnh mới";
                    }
                    else // Reusable
                    {
                        subGroupCode = "Reusable";
                        subGroupName = "Lĩnh tái sử dụng";
                        state = "Lĩnh tái sử dụng";
                    }
                    break;

                case QuotaBasedMaterial.SupportAccessories:
                    groupCode = "SupportAccessories";
                    groupName = "Phụ kiện chống lò";
                    materialTypeName = "Phụ kiện chống lò";
                    hasSubGroups = true;

                    if (item.QuotaBasedMaterialType == QuotaBasedMaterialType.New)
                    {
                        subGroupCode = "New";
                        subGroupName = "Lĩnh mới";
                        state = "Lĩnh mới";
                    }
                    else // Reusable
                    {
                        subGroupCode = "Reusable";
                        subGroupName = "Lĩnh tái sử dụng";
                        state = "Lĩnh tái sử dụng";
                    }
                    break;

                case QuotaBasedMaterial.MineTimber:
                    groupCode = "MineTimber";
                    groupName = "Gỗ lò";
                    materialTypeName = "Gỗ lò";
                    hasSubGroups = false;
                    break;

                default:
                    groupCode = "VTK";
                    groupName = "Vật tư khác";
                    materialTypeName = "Vật tư theo hạn mức";
                    hasSubGroups = false;
                    break;
            }

            // Tạo hoặc lấy MaterialGroup
            if (!materialGroups.ContainsKey(groupCode))
            {
                materialGroups[groupCode] = new MaterialGroupDto
                {
                    GroupCode = groupCode,
                    GroupName = groupName,
                    MaterialType = materialTypeName,
                    Materials = new(),
                    SubGroups = new()
                };
            }

            var plannedUnitPrice = GetUnitPrice(item.Material.Costs, productionOutput.StartMonth);
            var endingQuantity = item.IssuedQuantity - item.ShippedQuantity;

            var materialDetail = new MaterialDetailDto
            {
                MaterialId = item.Material.Id,
                MaterialCode = item.Material.Code?.Value ?? "",
                MaterialName = item.Material.Name,
                UnitOfMeasureName = item.Material.UnitOfMeasure?.Name ?? "",
                PlannedUnitPrice = plannedUnitPrice,
                ActualUnitPrice = 0,
                State = state,
                IssuedInPeriod = new IssuedInPeriodDto
                {
                    Received = new ReceivedSuppliesDto
                    {
                        Quantity = item.IssuedQuantity,
                        PlannedAmount = (decimal)item.IssuedQuantity * plannedUnitPrice,
                        ActualAmount = 0
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.IssuedQuantity,
                        Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
                    }
                },
                ExportedInPeriod = new ExportedInPeriodDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = item.ShippedQuantity,
                        Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                    }
                },
                EndingInventory = new EndingInventoryDto
                {
                    ExportedToProduction = new ExportedToProductionDto
                    {
                        Quantity = endingQuantity,
                        Amount = (decimal)endingQuantity * plannedUnitPrice
                    },
                    Total = new TotalDto
                    {
                        Quantity = endingQuantity,
                        Amount = (decimal)endingQuantity * plannedUnitPrice
                    }
                }
            };

            // Thêm vào SubGroup hoặc Materials trực tiếp
            if (hasSubGroups && subGroupCode != null && subGroupName != null)
            {
                // Tìm hoặc tạo SubGroup
                var subGroup = materialGroups[groupCode].SubGroups
                    .FirstOrDefault(sg => sg.SubGroupCode == subGroupCode);

                if (subGroup == null)
                {
                    subGroup = new SubGroupDto
                    {
                        SubGroupCode = subGroupCode,
                        SubGroupName = subGroupName,
                        Materials = new()
                    };
                    materialGroups[groupCode].SubGroups.Add(subGroup);
                }

                subGroup.Materials.Add(materialDetail);
            }
            else
            {
                // Không có subgroup (MineTimber) → thêm trực tiếp vào Materials
                materialGroups[groupCode].Materials.Add(materialDetail);
            }
        }

        // Sắp xếp SubGroups theo thứ tự: New trước, Reusable sau
        foreach (var group in materialGroups.Values)
        {
            if (group.SubGroups.Any())
            {
                var sortedSubGroups = group.SubGroups
                    .OrderBy(sg => sg.SubGroupCode == "New" ? 0 : 1)
                    .ToList();

                group.SubGroups.Clear();
                foreach (var sg in sortedSubGroups)
                {
                    group.SubGroups.Add(sg);
                }
            }
        }

        return new ProductionOutputDetailItemDto
        {
            CategoryType = 3,
            CategoryName = "Vật tư theo hạn mức",
            MaterialGroups = materialGroups.Values
                .OrderBy(g => g.GroupCode)
                .ToList()
        };
    }

    private async Task<ProductionOutputDetailItemDto> BuildAssetCategory(
        AcceptanceReport acceptanceReport, List<AcceptanceReport> previousAcceptanceReports, ProductionOutput productionOutput, CancellationToken cancellationToken)
    {
        var categoryItems = acceptanceReport.AcceptanceReportItems
            .Where(i => i.Asset != Asset.None)
            .ToList();

        var materialGroups = new Dictionary<string, MaterialGroupDto>();
        var groupCode = "ASSET";
        var groupName = "Tài sản";

        if (categoryItems.Any())
        {
            materialGroups[groupCode] = new MaterialGroupDto
            {
                GroupCode = groupCode,
                GroupName = groupName,
                MaterialType = "Tài sản",
                Materials = new()
            };

            foreach (var item in categoryItems)
            {
                if (item.Material == null)
                {
                    continue;
                }

                var plannedUnitPrice = GetUnitPrice(item.Material.Costs, productionOutput.StartMonth);
                var materialDetail = new MaterialDetailDto
                {
                    MaterialId = item.Material.Id,
                    MaterialCode = item.Material.Code?.Value ?? "",
                    MaterialName = item.Material.Name,
                    UnitOfMeasureName = item.Material.UnitOfMeasure?.Name ?? "",
                    PlannedUnitPrice = plannedUnitPrice,
                    ActualUnitPrice = 0,
                    IssuedInPeriod = new IssuedInPeriodDto
                    {
                        Received = new ReceivedSuppliesDto
                        {
                            Quantity = item.IssuedQuantity,
                            PlannedAmount = (decimal)item.IssuedQuantity * plannedUnitPrice,
                            ActualAmount = 0
                        },
                        Total = new TotalDto
                        {
                            Quantity = item.IssuedQuantity,
                            Amount = (decimal)item.IssuedQuantity * plannedUnitPrice
                        }
                    },
                    ExportedInPeriod = new ExportedInPeriodDto
                    {
                        ExportedToProduction = new ExportedToProductionDto
                        {
                            Quantity = item.ShippedQuantity,
                            Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                        },
                        Total = new TotalDto
                        {
                            Quantity = item.ShippedQuantity,
                            Amount = (decimal)item.ShippedQuantity * plannedUnitPrice
                        }
                    }
                };

                materialGroups[groupCode].Materials.Add(materialDetail);
            }
        }

        return new ProductionOutputDetailItemDto
        {
            CategoryType = 4,
            CategoryName = "Tài sản",
            MaterialGroups = materialGroups.Values.ToList()
        };
    }

    private decimal GetUnitPrice(IReadOnlyCollection<Cost> costs, DateOnly month)
    {
        return (decimal)(costs?.FirstOrDefault(c =>
            c.StartMonth <= month && c.EndMonth >= month)?.Amount ?? 0);
    }
}
