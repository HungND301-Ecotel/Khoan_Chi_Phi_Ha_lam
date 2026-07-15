using Application.Common.Caching;
using Application.Common.Events;
using Application.Common.Interfaces;
using Domain.Common.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Index;
using Domain.Entities.Pricing;
using Domain.Entities.Pricing.EletricityUnitPrice;
using Domain.Entities.Pricing.MaterialUnitPrice;
using Domain.Entities.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EfCore.Persistence.Context;

public class ApplicationDbContext(
    ICurrentUser currentUser,
    ISerializerService serializer,
    IOptions<DatabaseSettings> dbSettings,
    IEventPublisher events,
    ICacheService cacheService)
    : BaseDbContext(currentUser,
        serializer,
        dbSettings,
        events,
        cacheService)
{
    private static readonly DateTimeOffset FixedKeySeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid FixedKeyTunnelingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FixedKeyLongwallId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid FixedKeyRoadwaySlashingId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid FixedKeyAdjustmentFactorK1Id = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid FixedKeyAdjustmentFactorK2Id = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid FixedKeyAdjustmentFactorK3Id = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid FixedKeyAdjustmentFactorK4Id = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid FixedKeyAdjustmentFactorK5Id = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid FixedKeyAdjustmentFactorK6Id = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid FixedKeyAdjustmentFactorK7Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FixedKeyAdjustmentFactorK8Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // Identity DbSets

    #region Index

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AssignmentCode> AssignmentCodes => Set<AssignmentCode>();
    public DbSet<AssignmentCodeMaterial> AssignmentCodeMaterials => Set<AssignmentCodeMaterial>();
    public DbSet<Cost> Costs => Set<Cost>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<FixedKey> FixedKeys => Set<FixedKey>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<ProcessGroup> ProcessGroups => Set<ProcessGroup>();
    public DbSet<ProductionProcess> ProductionProcesses => Set<ProductionProcess>();
    public DbSet<Hardness> Hardnesses => Set<Hardness>();
    public DbSet<Power> Powers => Set<Power>();
    public DbSet<StoneClampRatio> StoneClampRatios => Set<StoneClampRatio>();
    public DbSet<InsertItem> InsertItems => Set<InsertItem>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<Technology> Technologys => Set<Technology>();
    public DbSet<SupportStep> SupportSteps => Set<SupportStep>();
    public DbSet<Passport> Passports => Set<Passport>();
    public DbSet<CuttingThickness> CuttingThicknesses => Set<CuttingThickness>();
    public DbSet<SeamFace> SeamFaces => Set<SeamFace>();
    public DbSet<LongwallParameters> LongwallParameters => Set<LongwallParameters>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AdjustmentFactorDescription> AdjustmentFactorDescriptions => Set<AdjustmentFactorDescription>();
    public DbSet<AdjustmentFactor> AdjustmentFactors => Set<AdjustmentFactor>();
    public DbSet<NormFactor> NormFactors => Set<NormFactor>();
    public DbSet<NormFactorAssignmentCode> NormFactorAssignmentCodes => Set<NormFactorAssignmentCode>();
    public DbSet<Code> Codes => Set<Code>();
    public DbSet<SavingsRateConfig> SavingsRateConfigs => Set<SavingsRateConfig>();
    public DbSet<AkFactorConfig> AkFactorConfigs => Set<AkFactorConfig>();
    public DbSet<RevenueCostAdjustmentConfig> RevenueCostAdjustmentConfigs => Set<RevenueCostAdjustmentConfig>();

    #endregion

    #region Pricing
    public DbSet<MaterialUnitPrice> MaterialUnitPrices => Set<MaterialUnitPrice>();
    public DbSet<SlideUnitPrice> SlideUnitPrices => Set<SlideUnitPrice>();
    public DbSet<MaintainUnitPrice> MaintainUnitPrices => Set<MaintainUnitPrice>();
    public DbSet<LowValuePerishableSupplyUnitPrice> LowValuePerishableSupplyUnitPrices => Set<LowValuePerishableSupplyUnitPrice>();
    public DbSet<SlideUnitPriceAssignmentCode> SlideUnitPriceAssignmentCodes => Set<SlideUnitPriceAssignmentCode>();
    public DbSet<MaintainUnitPriceEquipment> MaintainUnitPriceEquipments => Set<MaintainUnitPriceEquipment>();
    public DbSet<ElectricityUnitPriceEquipment> ElectricityUnitPriceEquipments => Set<ElectricityUnitPriceEquipment>();
    public DbSet<ProductUnitPrice> ProductUnitPrices => Set<ProductUnitPrice>();
    public DbSet<PlannedMaterialCost> PlannedMaterialCosts => Set<PlannedMaterialCost>();
    public DbSet<PlannedMaintainCost> PlannedMaintainCosts => Set<PlannedMaintainCost>();
    public DbSet<PlannedMaintainCostAdjustmentFactor> PlannedMaintainCostAdjustmentFactors => Set<PlannedMaintainCostAdjustmentFactor>();
    public DbSet<PlannedElectricityCost> PlannedElectricityCosts => Set<PlannedElectricityCost>();
    public DbSet<PlannedElectricityCostAdjustmentFactor> PlannedElectricityCostAdjustmentFactors => Set<PlannedElectricityCostAdjustmentFactor>();
    public DbSet<Output> Outputs => Set<Output>();
    public DbSet<ProductUnitPriceProductionOutput> ProductUnitPriceProductionOutputs => Set<ProductUnitPriceProductionOutput>();
    #endregion

    #region Production
    public DbSet<ProductionOutput> ProductionOutputs => Set<ProductionOutput>();
    public DbSet<ProductionOutputProcessGroup> ProductionOutputProcessGroups => Set<ProductionOutputProcessGroup>();
    public DbSet<ProductionOutputProduct> ProductionOutputProducts => Set<ProductionOutputProduct>();
    public DbSet<AcceptanceReport> AcceptanceReports => Set<AcceptanceReport>();
    public DbSet<ActualElectricityCost> ActualElectricityCosts => Set<ActualElectricityCost>();
    public DbSet<ActualEletricityEquipment> ActualEletricityEquipments => Set<ActualEletricityEquipment>();
    public DbSet<AcceptanceReportItem> AcceptanceReportItems => Set<AcceptanceReportItem>();
    public DbSet<AcceptanceReportItemCategoryAllocation> AcceptanceReportItemCategoryAllocations => Set<AcceptanceReportItemCategoryAllocation>();
    public DbSet<AcceptanceReportItemCategoryAllocationEquipment> AcceptanceReportItemCategoryAllocationEquipments => Set<AcceptanceReportItemCategoryAllocationEquipment>();
    public DbSet<AcceptanceReportItemShippedDetail> AcceptanceReportItemShippedDetails => Set<AcceptanceReportItemShippedDetail>();
    public DbSet<AcceptanceReportItemIssuedDetail> AcceptanceReportItemIssuedDetails => Set<AcceptanceReportItemIssuedDetail>();
    public DbSet<AcceptanceReportItemLog> AcceptanceReportItemLogs => Set<AcceptanceReportItemLog>();
    public DbSet<LongTermAnchorSeed> LongTermAnchorSeeds => Set<LongTermAnchorSeed>();
    public DbSet<LongTermAnchorSeedItem> LongTermAnchorSeedItems => Set<LongTermAnchorSeedItem>();
    public DbSet<LongTermAnchorSeedItemLog> LongTermAnchorSeedItemLogs => Set<LongTermAnchorSeedItemLog>();
    public DbSet<LongTermAnchorSeedProcessGroupMetric> LongTermAnchorSeedProcessGroupMetrics => Set<LongTermAnchorSeedProcessGroupMetric>();
    public DbSet<LumpSumQuarterCustomCost> LumpSumQuarterCustomCosts => Set<LumpSumQuarterCustomCost>();
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var decimalProps = modelBuilder.Model
            .GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => (System.Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(decimal));

        foreach (var property in decimalProps)
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        #region Index
        //add to Index Schema
        modelBuilder.Entity<AssignmentCode>().ToTable(nameof(AssignmentCode), "Index");
        modelBuilder.Entity<AssignmentCodeMaterial>().ToTable(nameof(AssignmentCodeMaterial), "Index");
        modelBuilder.Entity<Cost>().ToTable(nameof(Cost), "Index");
        modelBuilder.Entity<Material>().ToTable(nameof(Material), "Index");
        modelBuilder.Entity<Department>().ToTable(nameof(Department), "Index");
        modelBuilder.Entity<Employee>().ToTable(nameof(Employee), "Index");
        modelBuilder.Entity<Position>().ToTable(nameof(Position), "Index");
        modelBuilder.Entity<FixedKey>().ToTable(nameof(FixedKey), "Index");
        modelBuilder.Entity<UnitOfMeasure>().ToTable(nameof(UnitOfMeasure), "Index");
        modelBuilder.Entity<ProcessGroup>().ToTable(nameof(ProcessGroup), "Index");
        modelBuilder.Entity<ProductionProcess>().ToTable(nameof(ProductionProcess), "Index");
        modelBuilder.Entity<Hardness>().ToTable(nameof(Hardness), "Index");
        modelBuilder.Entity<StoneClampRatio>().ToTable(nameof(StoneClampRatio), "Index");
        modelBuilder.Entity<InsertItem>().ToTable(nameof(InsertItem), "Index");
        modelBuilder.Entity<ProductionOrder>().ToTable(nameof(ProductionOrder), "Index");
        modelBuilder.Entity<Technology>().ToTable(nameof(Technology), "Index");
        modelBuilder.Entity<SavingsRateConfig>().ToTable(nameof(SavingsRateConfig), "Index");
        modelBuilder.Entity<AkFactorConfig>().ToTable(nameof(AkFactorConfig), "Index");
        modelBuilder.Entity<RevenueCostAdjustmentConfig>().ToTable(nameof(RevenueCostAdjustmentConfig), "Index");
        modelBuilder.Entity<Power>().ToTable(nameof(Power), "Index");
        modelBuilder.Entity<SupportStep>().ToTable(nameof(SupportStep), "Index");
        modelBuilder.Entity<Passport>().ToTable(nameof(Passport), "Index");
        modelBuilder.Entity<LongwallParameters>().ToTable(nameof(LongwallParameters), "Index");
        modelBuilder.Entity<CuttingThickness>().ToTable(nameof(CuttingThickness), "Index");
        modelBuilder.Entity<SeamFace>().ToTable(nameof(SeamFace), "Index");
        modelBuilder.Entity<Product>().ToTable(nameof(Product), "Index");
        modelBuilder.Entity<AdjustmentFactor>().ToTable(nameof(AdjustmentFactor), "Index");
        modelBuilder.Entity<AdjustmentFactorDescription>().ToTable(nameof(AdjustmentFactorDescription), "Index");
        modelBuilder.Entity<NormFactor>().ToTable(nameof(NormFactor), "Index");
        modelBuilder.Entity<NormFactorAssignmentCode>().ToTable(nameof(NormFactorAssignmentCode), "Index");
        modelBuilder.Entity<Code>().ToTable(nameof(Code), "Index");
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactorDescription>()
            .ToTable(nameof(PlannedMaintainCostAdjustmentFactorDescription), "Index", tb => tb.HasCheckConstraint(
                "CK_PlannedMaintainCostAdjustmentFactorDescription_CustomOrReference",
                @"
                    (
                        (
                            ""AdjustmentFactorDescriptionId"" IS NOT NULL AND
                            ""AdjustmentFactorId"" IS NULL AND
                            ""CustomValue"" IS NULL
                        )
                        OR
                        (
                            ""AdjustmentFactorDescriptionId"" IS NULL AND
                            ""AdjustmentFactorId"" IS NOT NULL AND
                            ""CustomValue"" IS NOT NULL
                        )
                    )
                "));
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactorDescription>()
            .ToTable(nameof(PlannedElectricityCostAdjustmentFactorDescription), "Index", tb => tb.HasCheckConstraint(
                "CK_PlannedElectricityCostAdjustmentFactorDescription_CustomOrReference",
                @"
                    (
                        (
                            ""AdjustmentFactorDescriptionId"" IS NOT NULL AND
                            ""AdjustmentFactorId"" IS NULL AND
                            ""CustomValue"" IS NULL
                        )
                        OR
                        (
                            ""AdjustmentFactorDescriptionId"" IS NULL AND
                            ""AdjustmentFactorId"" IS NOT NULL AND
                            ""CustomValue"" IS NOT NULL
                        )
                    )
                "));


        // Assignment Code table
        modelBuilder.Entity<AssignmentCode>()
            .HasOne(s => s.UnitOfMeasure)
            .WithMany(h => h.AssignmentCodes)
            .HasForeignKey(s => s.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssignmentCode>()
            .HasOne(s => s.Code)
            .WithOne(h => h.AssignmentCode)
            .HasForeignKey<AssignmentCode>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssignmentCode>()
            .HasMany(s => s.Costs)
            .WithOne(h => h.AssignmentCode)
            .HasForeignKey(s => s.AssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssignmentCode>()
            .HasMany(s => s.AssignmentCodeMaterials)
            .WithOne(h => h.AssignmentCode)
            .HasForeignKey(s => s.AssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssignmentCode>()
            .HasMany(s => s.MaterialUnitPriceAssignmentCodes)
            .WithOne(h => h.AssignmentCode)
            .HasForeignKey(s => s.AssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AssignmentCode>()
            .HasMany(s => s.NormFactorAssignmentCodes)
            .WithOne(h => h.AssignmentCode)
            .HasForeignKey(s => s.AssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Material table
        modelBuilder.Entity<Material>()
            .HasOne(s => s.AssignmentCode)
            .WithMany(h => h.Materials)
            .HasForeignKey(s => s.AssigmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Material>()
            .HasOne(s => s.Code)
            .WithOne(h => h.Material)
            .HasForeignKey<Material>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Material>()
            .HasOne(s => s.UnitOfMeasure)
            .WithMany(h => h.Materials)
            .HasForeignKey(s => s.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Material>()
            .HasMany(s => s.Costs)
            .WithOne(h => h.Material)
            .HasForeignKey(s => s.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Material>()
            .HasMany(s => s.AssignmentCodeMaterials)
            .WithOne(h => h.Material)
            .HasForeignKey(s => s.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentCodeMaterial>()
            .Property(e => e.Role)
            .HasDefaultValue(AssignmentCodeMaterialRole.Material)
            .HasSentinel(AssignmentCodeMaterialRole.Material);

        modelBuilder.Entity<AssignmentCodeMaterial>()
            .HasIndex(e => new { e.AssignmentCodeId, e.MaterialId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        // ProductionOrder table
        modelBuilder.Entity<ProductionOrder>()
            .HasOne(s => s.Code)
            .WithOne(h => h.ProductionOrder)
            .HasForeignKey<ProductionOrder>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignmentCode>()
            .HasMany(l => l.ActualEletricityEquipment)
            .WithOne(l => l.Equipment)
            .HasForeignKey(l => l.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        //Cost table
        modelBuilder.Entity<Cost>()
            .ToTable(tb => tb.HasCheckConstraint(
                "CK_Cost_OneParentOnly",
                @"
                    (
                        (CASE WHEN ""MaterialId""  IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN ""AssignmentCodeId"" IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN ""EquipmentId"" IS NOT NULL THEN 1 ELSE 0 END) +
                        (CASE WHEN ""PartId"" IS NOT NULL THEN 1 ELSE 0 END)
                    ) = 1
                "
                ));


        // Fixed Key table
        modelBuilder.Entity<FixedKey>()
            .Property(x => x.Key)
            .HasMaxLength(50);
        modelBuilder.Entity<FixedKey>()
            .HasIndex(x => x.Key)
            .IsUnique();
        modelBuilder.Entity<FixedKey>()
            .HasData(
                new
                {
                    Id = FixedKeyTunnelingId,
                    Key = "DL",
                    Name = "Đào lò",
                    Type = FixedKeyType.DL,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyLongwallId,
                    Key = "LC",
                    Name = "Lò chợ",
                    Type = FixedKeyType.LC,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyRoadwaySlashingId,
                    Key = "XL",
                    Name = "Xén lò",
                    Type = FixedKeyType.XL,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK1Id,
                    Key = "K1",
                    Name = "Hệ số điều chỉnh K1",
                    Type = FixedKeyType.K1,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK2Id,
                    Key = "K2",
                    Name = "Hệ số điều chỉnh K2",
                    Type = FixedKeyType.K2,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK3Id,
                    Key = "K3",
                    Name = "Hệ số điều chỉnh K3",
                    Type = FixedKeyType.K3,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK4Id,
                    Key = "K4",
                    Name = "Hệ số điều chỉnh K4",
                    Type = FixedKeyType.K4,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK5Id,
                    Key = "K5",
                    Name = "Hệ số điều chỉnh K5",
                    Type = FixedKeyType.K5,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK6Id,
                    Key = "K6",
                    Name = "Hệ số điều chỉnh K6",
                    Type = FixedKeyType.K6,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK7Id,
                    Key = "K7",
                    Name = "Hệ số điều chỉnh K7",
                    Type = FixedKeyType.K7,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                },
                new
                {
                    Id = FixedKeyAdjustmentFactorK8Id,
                    Key = "K8",
                    Name = "Hệ số điều chỉnh K8",
                    Type = FixedKeyType.K8,
                    CreatedBy = 0L,
                    CreatedOn = FixedKeySeedTimestamp,
                    LastModifiedBy = 0L,
                    LastModifiedOn = FixedKeySeedTimestamp,
                    DeletedBy = (long?)null,
                    DeletedOn = (DateTimeOffset?)null,
                });

        // Process Group table
        modelBuilder.Entity<ProcessGroup>()
            .HasMany(s => s.ProductionProcesses)
            .WithOne(h => h.ProcessGroup)
            .HasForeignKey(s => s.ProcessGroupId);
        modelBuilder.Entity<ProcessGroup>()
            .HasOne(s => s.Code)
            .WithOne(h => h.ProcessGroup)
            .HasForeignKey<ProcessGroup>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProcessGroup>()
            .HasOne(s => s.FixedKey)
            .WithMany(h => h.ProcessGroups)
            .HasForeignKey(s => s.FixedKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AkFactorConfig>()
            .HasOne(x => x.ProcessGroup)
            .WithMany()
            .HasForeignKey(x => x.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AkFactorConfig>()
            .Property(x => x.AkDiffValue)
            .HasPrecision(18, 4);
        modelBuilder.Entity<AkFactorConfig>()
            .Property(x => x.AdjustmentRate)
            .HasPrecision(18, 4);

        // Department table
        modelBuilder.Entity<Department>()
            .HasOne(s => s.Code)
            .WithOne(h => h.Department)
            .HasForeignKey<Department>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Production Process table
        modelBuilder.Entity<ProductionProcess>()
            .HasOne(s => s.Code)
            .WithOne(h => h.ProductionProcess)
            .HasForeignKey<ProductionProcess>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Adjustment Factor table
        modelBuilder.Entity<AdjustmentFactor>()
            .HasMany(s => s.AdjustmentFactorDescriptions)
            .WithOne(h => h.AdjustmentFactor)
            .HasForeignKey(s => s.AdjustmentFactorId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AdjustmentFactor>()
            .HasOne(s => s.ProcessGroup)
            .WithMany(h => h.AdjustmentFactors)
            .HasForeignKey(s => s.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AdjustmentFactor>()
            .HasOne(s => s.FixedKey)
            .WithMany(h => h.AdjustmentFactors)
            .HasForeignKey(s => s.FixedKeyId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AdjustmentFactor>()
            .HasOne(s => s.Code)
            .WithOne(h => h.AdjustmentFactor)
            .HasForeignKey<AdjustmentFactor>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Adjustment Factor Description table
        modelBuilder.Entity<AdjustmentFactorDescription>()
            .HasIndex(e => e.AdjustmentFactorId);

        //NormFactor table
        modelBuilder.Entity<NormFactor>()
            .HasOne(s => s.ProductionProcess)
            .WithMany(h => h.NormFactors)
            .HasForeignKey(s => s.ProductionProcessId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NormFactor>()
            .HasOne(s => s.StoneClampRatio)
            .WithMany(h => h.NormFactors)
            .HasForeignKey(s => s.StoneClampRatioId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NormFactor>()
            .HasOne(s => s.Hardness)
            .WithMany(h => h.NormFactors)
            .HasForeignKey(s => s.HardnessId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NormFactor>()
            .HasMany(s => s.NormFactorAssignmentCodes)
            .WithOne(h => h.NormFactor)
            .HasForeignKey(s => s.NormFactorId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<NormFactorAssignmentCode>()
            .HasIndex(e => new { e.NormFactorId, e.AssignmentCodeId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<NormFactorAssignmentCode>()
            .HasOne(s => s.TargetHardness)
            .WithMany(h => h.TargetedNormFactorAssignmentCodes)
            .HasForeignKey(s => s.TargetHardnessId)
            .OnDelete(DeleteBehavior.SetNull);

        // Product table
        modelBuilder.Entity<Product>()
            .HasOne(s => s.ProcessGroup)
            .WithMany(h => h.Products)
            .HasForeignKey(s => s.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Product>()
            .HasOne(s => s.Code)
            .WithOne(h => h.Product)
            .HasForeignKey<Product>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        // StoneClampRatio table
        modelBuilder.Entity<StoneClampRatio>()
            .HasIndex(e => e.Value)
            .HasFilter("\"DeletedOn\" IS NULL");


        //Code table
        modelBuilder.Entity<Code>()
            .HasIndex(e => e.Value)
            .HasFilter("\"DeletedOn\" IS NULL");

        //PlannedMaintainCostAdjustmentFactorDescription table
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactorDescription>()
            .HasOne(m => m.AdjustmentFactorDescription)
            .WithMany(h => h.PlannedMaintainCostAdjustmentFactorDescriptions)
            .HasForeignKey(s => s.AdjustmentFactorDescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactorDescription>()
            .HasOne(m => m.AdjustmentFactor)
            .WithMany()
            .HasForeignKey(s => s.AdjustmentFactorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactorDescription>()
            .HasOne(m => m.PlannedMaintainCostAdjustmentFactor)
            .WithMany(h => h.PlannedMaintainCostAdjustmentFactorDescriptions)
            .HasForeignKey(s => s.PlannedMaintainCostAdjustmentFactorId)
            .OnDelete(DeleteBehavior.Cascade);

        //PlannedElectricityCostAdjustmentFactorDescription table
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactorDescription>()
            .HasOne(m => m.AdjustmentFactorDescription)
            .WithMany(h => h.PlannedElectricityCostAdjustmentFactorDescriptions)
            .HasForeignKey(s => s.AdjustmentFactorDescriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactorDescription>()
            .HasOne(m => m.AdjustmentFactor)
            .WithMany()
            .HasForeignKey(s => s.AdjustmentFactorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactorDescription>()
            .HasOne(m => m.PlannedElectricityCostAdjustmentFactor)
            .WithMany(h => h.PlannedElectricityCostAdjustmentFactorDescriptions)
            .HasForeignKey(s => s.PlannedElectricityCostAdjustmentFactorId)
            .OnDelete(DeleteBehavior.Cascade);

        //Hardness table
        modelBuilder.Entity<Hardness>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //Power table
        modelBuilder.Entity<Power>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //InsertItem table
        modelBuilder.Entity<InsertItem>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //Technology table
        modelBuilder.Entity<Technology>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //SeamFace table
        modelBuilder.Entity<SeamFace>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //SupportStep table
        modelBuilder.Entity<SupportStep>()
            .HasIndex(e => e.Value)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //Passport table
        modelBuilder.Entity<Passport>()
            .HasIndex(e => new { e.Name, e.Sd, e.Sc })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //Passport table
        modelBuilder.Entity<LongwallParameters>()
            .HasIndex(e => new { e.Llc, e.Lkc, e.Mk })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        //UnitOfMeasure table
        modelBuilder.Entity<UnitOfMeasure>()
            .HasIndex(e => e.Name)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        #endregion


        #region Pricing
        //add to Pricing schema
        // Configure TPH inheritance for MaterialUnitPrice
        modelBuilder.Entity<MaterialUnitPrice>()
            .ToTable(nameof(MaterialUnitPrice), "Pricing")
            .HasDiscriminator<MaterialUnitPriceType>("MaterialType")
            .HasValue<TunnelExcavationMaterialUnitPrice>(MaterialUnitPriceType.TunnelExcavation)
            .HasValue<LongwallMaterialUnitPrice>(MaterialUnitPriceType.Longwall)
            .HasValue<TunnelSupportAndDrillingMaterialUnitPrice>(MaterialUnitPriceType.TunnelSupportAndDrilling);

        modelBuilder.Entity<MaterialUnitPriceAssignmentCode>().ToTable(nameof(MaterialUnitPriceAssignmentCode), "Pricing");
        modelBuilder.Entity<SlideUnitPrice>().ToTable(nameof(SlideUnitPrice), "Pricing");
        modelBuilder.Entity<MaintainUnitPrice>().ToTable(nameof(MaintainUnitPrice), "Pricing");
        modelBuilder.Entity<LowValuePerishableSupplyUnitPrice>().ToTable(nameof(LowValuePerishableSupplyUnitPrice), "Pricing");
        modelBuilder.Entity<SlideUnitPriceAssignmentCode>().ToTable(nameof(SlideUnitPriceAssignmentCode), "Pricing");
        modelBuilder.Entity<MaintainUnitPriceEquipment>().ToTable(nameof(MaintainUnitPriceEquipment), "Pricing");

        // Configure TPH inheritance for ElectricityUnitPriceEquipment
        modelBuilder.Entity<ElectricityUnitPriceEquipment>()
            .ToTable("ElectricityUnitPriceEquipment", "Pricing")
            .HasDiscriminator<ElectricityUnitPriceType>("ElectricityType")
            .HasValue<TunnelElectricityUnitPriceEquipment>(ElectricityUnitPriceType.TunnelExcavation)
            .HasValue<TrimmingElectricityUnitPriceEquipment>(ElectricityUnitPriceType.Trimming)
            .HasValue<LongwallElectricityUnitPriceEquipment>(ElectricityUnitPriceType.Longwall);

        modelBuilder.Entity<ProductUnitPrice>().ToTable(nameof(ProductUnitPrice), "Pricing");
        modelBuilder.Entity<PlannedMaintainCost>().ToTable(nameof(PlannedMaintainCost), "Pricing");
        modelBuilder.Entity<PlannedMaterialCost>().ToTable(nameof(PlannedMaterialCost), "Pricing");
        modelBuilder.Entity<PlannedElectricityCost>().ToTable(nameof(PlannedElectricityCost), "Pricing");
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactor>().ToTable(nameof(PlannedMaintainCostAdjustmentFactor), "Pricing");
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactor>().ToTable(nameof(PlannedElectricityCostAdjustmentFactor), "Pricing");
        modelBuilder.Entity<Output>().ToTable(nameof(Output), "Pricing");

        //MaterialUnitPrices table - Base configuration
        modelBuilder.Entity<MaterialUnitPrice>()
            .HasOne(s => s.Technology)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.TechnologyId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MaterialUnitPrice>()
            .HasOne(s => s.Code)
            .WithOne(h => h.MaterialUnitPrice)
            .HasForeignKey<MaterialUnitPrice>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MaterialUnitPrice>()
            .HasOne(s => s.ProductionProcess)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.ProcessId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MaterialUnitPrice>()
            .HasMany(s => s.MaterialUnitPriceAssignmentCodes)
            .WithOne(h => h.MaterialUnitPrice)
            .HasForeignKey(s => s.MaterialUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MaterialUnitPrice>()
            .Navigation(s => s.MaterialUnitPriceAssignmentCodes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        //TunnelExcavationMaterialUnitPrice - Đào lò specific configuration
        modelBuilder.Entity<TunnelExcavationMaterialUnitPrice>()
            .HasOne(s => s.Passport)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.PassportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TunnelExcavationMaterialUnitPrice>()
            .HasOne(s => s.Hardness)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.HardnessId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TunnelExcavationMaterialUnitPrice>()
            .HasOne(s => s.InsertItem)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.InsertItemId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TunnelExcavationMaterialUnitPrice>()
            .HasOne(s => s.SupportStep)
            .WithMany(h => h.MaterialUnitPrices)
            .HasForeignKey(s => s.SupportStepId)
            .OnDelete(DeleteBehavior.Cascade);

        // TunnelSupportAndDrillingMaterialUnitPrice - Chống xén specific configuration
        modelBuilder.Entity<TunnelSupportAndDrillingMaterialUnitPrice>()
            .HasOne(s => s.Passport)
            .WithMany()
            .HasForeignKey(s => s.PassportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<TunnelSupportAndDrillingMaterialUnitPrice>()
            .HasOne(s => s.Hardness)
            .WithMany()
            .HasForeignKey(s => s.HardnessId)
            .OnDelete(DeleteBehavior.Cascade);

        //LongwallMaterialUnitPrice - Lò chợ specific configuration
        modelBuilder.Entity<LongwallMaterialUnitPrice>()
            .HasOne(s => s.LongwallParameters)
            .WithMany(s => s.MaterialUnitPrices)
            .HasForeignKey(s => s.LongwallParametersId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LongwallMaterialUnitPrice>()
            .HasOne(s => s.CuttingThickness)
            .WithMany(s => s.MaterialUnitPrices)
            .HasForeignKey(s => s.CuttingThicknessId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LongwallMaterialUnitPrice>()
            .HasOne(s => s.SeamFace)
            .WithMany(s => s.MaterialUnitPrices)
            .HasForeignKey(s => s.SeamFaceId)
            .OnDelete(DeleteBehavior.Cascade);

        //SLideUnitPrices table
        modelBuilder.Entity<SlideUnitPrice>()
            .HasMany(s => s.SlideUnitPriceAssignmentCodes)
            .WithOne(h => h.SlideUnitPrice)
            .HasForeignKey(s => s.SlideUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlideUnitPrice>()
            .HasOne(s => s.ProcessGroup)
            .WithMany(h => h.SlideUnitPrices)
            .HasForeignKey(s => s.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlideUnitPrice>()
            .HasOne(s => s.Passport)
            .WithMany(h => h.SlideUnitPrices)
            .HasForeignKey(s => s.PassportId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlideUnitPrice>()
            .HasOne(s => s.Hardness)
            .WithMany(h => h.SlideUnitPrices)
            .HasForeignKey(s => s.HardnessId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlideUnitPrice>()
            .HasOne(s => s.Code)
            .WithOne(h => h.SlideUnitPrice)
            .HasForeignKey<SlideUnitPrice>(s => s.CodeId)
            .OnDelete(DeleteBehavior.Cascade);

        //SlideUnitPriceAssignmentCode table
        modelBuilder.Entity<SlideUnitPriceAssignmentCode>()
            .HasIndex(e => new { e.SlideUnitPriceId, e.MaterialId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<SlideUnitPriceAssignmentCode>()
            .HasOne(m => m.Material)
            .WithMany(h => h.SlideUnitPriceAssignmentCodes)
            .HasForeignKey(s => s.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SlideUnitPriceAssignmentCode>()
            .HasMany(m => m.PlannedMaterialCosts)
            .WithOne(h => h.SlideUnitPriceAssignmentCode)
            .HasForeignKey(s => s.SlideUnitPriceAssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);


        //MaterialUnitPriceAssignmentCode table
        modelBuilder.Entity<MaterialUnitPriceAssignmentCode>()
            .HasIndex(e => new { e.MaterialUnitPriceId, e.AssignmentCodeId, e.MaterialId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<MaterialUnitPriceAssignmentCode>()
            .HasIndex(e => e.MaterialId);
        modelBuilder.Entity<MaterialUnitPriceAssignmentCode>()
            .HasOne(e => e.Material)
            .WithMany()
            .HasForeignKey(e => e.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        //MaintainUnitPrice table
        modelBuilder.Entity<MaintainUnitPrice>()
            .HasIndex(e => new
            {
                e.EquipmentId,
                e.StartMonth,
                e.EndMonth
            })
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<MaintainUnitPrice>()
            .HasMany(m => m.MaintainUnitPriceEquipments)
            .WithOne(h => h.MaintainUnitPrice)
            .HasForeignKey(s => s.MaintainUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MaintainUnitPrice>()
            .HasOne(m => m.Equipment)
            .WithMany(h => h.MaintainUnitPrices)
            .HasForeignKey(s => s.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        //MaintainUnitPriceEquipment table
        modelBuilder.Entity<MaintainUnitPriceEquipment>()
            .HasIndex(e => new { e.MaintainUnitPriceId, e.PartId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<MaintainUnitPriceEquipment>()
            .HasOne(m => m.Part)
            .WithMany(h => h.MaintainUnitPriceEquipments)
            .HasForeignKey(s => s.PartId)
            .OnDelete(DeleteBehavior.Cascade);


        //MaintainUnitPrice table
        modelBuilder.Entity<LowValuePerishableSupplyUnitPrice>()
            .HasIndex(e => new
            {
                e.ProcessGroupId,
                e.DepartmentId,
                e.StartMonth,
                e.Type,
                e.EndMonth
            })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LowValuePerishableSupplyUnitPrice>()
            .HasOne(m => m.ProcessGroup)
            .WithMany(h => h.LowValuePerishableSupplyUnitPrices)
            .HasForeignKey(s => s.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LowValuePerishableSupplyUnitPrice>()
            .HasOne(m => m.Department)
            .WithMany(h => h.LowValuePerishableSupplyUnitPrices)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        //ElectricityUnitPriceEquipment table
        modelBuilder.Entity<ElectricityUnitPriceEquipment>()
            .HasIndex(e => e.EquipmentId)
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<ElectricityUnitPriceEquipment>()
            .HasOne(m => m.Equipment)
            .WithMany(h => h.ElectricityUnitPriceEquipments)
            .HasForeignKey(s => s.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        //ProductUnitPRice
        modelBuilder.Entity<ProductUnitPrice>()
            .HasIndex(e => new { e.ProductId, e.ScenarioType, e.DepartmentId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<ProductUnitPrice>()
            .HasOne(m => m.Product)
            .WithMany(h => h.ProductUnitPrices)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductUnitPrice>()
            .HasMany(m => m.Outputs)
            .WithOne(h => h.ProductUnitPrice)
            .HasForeignKey(s => s.ProductUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductUnitPrice>()
            .HasOne(m => m.UnitOfMeasure)
            .WithMany(h => h.ProductUnitPrices)
            .HasForeignKey(s => s.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductUnitPrice>()
            .HasOne(m => m.Department)
            .WithMany(d => d.ProductUnitPrices)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        //ProductUnitPriceProductionOutput - Many-to-Many relationship table
        modelBuilder.Entity<ProductUnitPriceProductionOutput>().ToTable(nameof(ProductUnitPriceProductionOutput), "Pricing");
        modelBuilder.Entity<ProductUnitPriceProductionOutput>()
            .HasKey(p => new { p.ProductUnitPriceId, p.ProductionOutputId });
        modelBuilder.Entity<ProductUnitPriceProductionOutput>()
            .HasOne(p => p.ProductUnitPrice)
            .WithMany(pu => pu.ProductUnitPriceProductionOutputs)
            .HasForeignKey(p => p.ProductUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductUnitPriceProductionOutput>()
            .HasOne(p => p.ProductionOutput)
            .WithMany(po => po.ProductUnitPriceProductionOutputs)
            .HasForeignKey(p => p.ProductionOutputId)
            .OnDelete(DeleteBehavior.Cascade);


        //PlannedMaterialCost
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasIndex(e => new { e.ProductUnitPriceId, e.OutputId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasIndex(e => e.OutputId)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.MaterialUnitPrice)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.MaterialUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.StoneClampRatio)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.StoneClampRatioReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.Material)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.MaterialReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.ProductUnitPrice)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.ProductUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.SlideUnitPriceAssignmentCode)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.SlideUnitPriceAssignmentCodeId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaterialCost>()
            .HasOne(m => m.NormFactor)
            .WithMany(h => h.PlannedMaterialCosts)
            .HasForeignKey(s => s.NormFactorId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Output>()
            .HasOne(o => o.PlannedMaterialCost)
            .WithOne(p => p.Output)
            .HasForeignKey<PlannedMaterialCost>(p => p.OutputId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Output>().HasKey(o => o.Id);
        modelBuilder.Entity<Output>().HasIndex(o => o.Id).HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<Output>()
            .Property(o => o.PlanAshContent)
            .HasDefaultValue(0d);

        //PlannedElectricityCost
        modelBuilder.Entity<PlannedElectricityCost>()
            .HasIndex(e => new { e.ProductUnitPriceId, e.OutputId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedElectricityCost>()
            .HasIndex(e => e.OutputId)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedElectricityCost>()
            .HasOne(m => m.ProductUnitPrice)
            .WithMany(h => h.PlannedElectricityCosts)
            .HasForeignKey(s => s.ProductUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Output>()
            .HasOne(o => o.PlannedElectricityCost)
            .WithOne(p => p.Output)
            .HasForeignKey<PlannedElectricityCost>(p => p.OutputId)
            .OnDelete(DeleteBehavior.Cascade);

        //PlannedElectricityCostAdjustmentFactor
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactor>()
            .HasOne(m => m.PlannedElectricityCost)
            .WithMany(h => h.PlannedElectricityCostAdjustmentFactors)
            .HasForeignKey(s => s.PlannedElectricityCostId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedElectricityCostAdjustmentFactor>()
            .HasOne(m => m.ElectricityUnitPriceEquipment)
            .WithMany(h => h.PlannedElectricityCostAdjustmentFactors)
            .HasForeignKey(s => s.ElectricityUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);

        //PlannedMaintainCost
        modelBuilder.Entity<PlannedMaintainCost>()
            .HasIndex(e => new { e.ProductUnitPriceId, e.OutputId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedMaintainCost>()
            .HasIndex(e => e.OutputId)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<PlannedMaintainCost>()
            .HasOne(m => m.ProductUnitPrice)
            .WithMany(h => h.PlannedMaintainCosts)
            .HasForeignKey(s => s.ProductUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Output>()
            .HasOne(o => o.PlannedMaintainCost)
            .WithOne(p => p.Output)
            .HasForeignKey<PlannedMaintainCost>(p => p.OutputId)
            .OnDelete(DeleteBehavior.Cascade);

        //PlannedMaintainCostAdjustmentFactor
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactor>()
            .HasOne(m => m.PlannedMaintainCost)
            .WithMany(h => h.PlannedMaintainCostAdjustmentFactors)
            .HasForeignKey(s => s.PlannedMaintainCostId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<PlannedMaintainCostAdjustmentFactor>()
            .HasOne(m => m.MaintainUnitPrice)
            .WithMany(h => h.PlannedMaintainCostAdjustmentFactors)
            .HasForeignKey(s => s.MaintainUnitPriceId)
            .OnDelete(DeleteBehavior.Cascade);

        //ActualElectricityCostAdjustmentFactor (remove - entity deleted)

        #endregion

        #region Production
        // Configure Production schema
        modelBuilder.Entity<ProductionOutput>().ToTable(nameof(ProductionOutput), "Production");
        modelBuilder.Entity<ProductionOutputProcessGroup>().ToTable(nameof(ProductionOutputProcessGroup), "Production");
        modelBuilder.Entity<ProductionOutputProduct>().ToTable(nameof(ProductionOutputProduct), "Production");
        modelBuilder.Entity<AcceptanceReport>().ToTable(nameof(AcceptanceReport), "Production");
        modelBuilder.Entity<AcceptanceReportItem>().ToTable(nameof(AcceptanceReportItem), "Production");
        modelBuilder.Entity<AcceptanceReportItemCategoryAllocation>().ToTable(nameof(AcceptanceReportItemCategoryAllocation), "Production");
        modelBuilder.Entity<AcceptanceReportItemCategoryAllocationEquipment>().ToTable(nameof(AcceptanceReportItemCategoryAllocationEquipment), "Production");
        modelBuilder.Entity<ActualElectricityCost>().ToTable(nameof(ActualElectricityCost), "Production");
        modelBuilder.Entity<ActualEletricityEquipment>().ToTable(nameof(ActualEletricityEquipment), "Production");
        modelBuilder.Entity<AcceptanceReportItemIssuedDetail>().ToTable(nameof(AcceptanceReportItemIssuedDetail), "Production");
        modelBuilder.Entity<AcceptanceReportItemShippedDetail>().ToTable(nameof(AcceptanceReportItemShippedDetail), "Production");
        modelBuilder.Entity<AcceptanceReportItemLog>().ToTable(nameof(AcceptanceReportItemLog), "Production");
        modelBuilder.Entity<LongTermAnchorSeed>().ToTable(nameof(LongTermAnchorSeed), "Production");
        modelBuilder.Entity<LongTermAnchorSeedItem>().ToTable(nameof(LongTermAnchorSeedItem), "Production");
        modelBuilder.Entity<LongTermAnchorSeedItemLog>().ToTable(nameof(LongTermAnchorSeedItemLog), "Production");
        modelBuilder.Entity<LongTermAnchorSeedProcessGroupMetric>().ToTable(nameof(LongTermAnchorSeedProcessGroupMetric), "Production");
        modelBuilder.Entity<LumpSumQuarterCustomCost>().ToTable(nameof(LumpSumQuarterCustomCost), "Production");

        modelBuilder.Entity<ProductionOutput>()
            .HasMany(p => p.ProductionOutputProcessGroups)
            .WithOne(g => g.ProductionOutput)
            .HasForeignKey(g => g.ProductionOutputId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ProductionOutput>()
            .HasOne(p => p.Department)
            .WithMany(d => d.ProductionOutputs)
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LongTermAnchorSeed>()
            .HasOne(s => s.Department)
            .WithMany(d => d.LongTermAnchorSeeds)
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeed>()
            .HasMany(s => s.Items)
            .WithOne(i => i.LongTermAnchorSeed)
            .HasForeignKey(i => i.LongTermAnchorSeedId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeed>()
            .HasMany(s => s.ProcessGroupMetrics)
            .WithOne(m => m.LongTermAnchorSeed)
            .HasForeignKey(m => m.LongTermAnchorSeedId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeed>()
            .HasIndex(s => s.DepartmentId)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasOne(i => i.ProcessGroup)
            .WithMany()
            .HasForeignKey(i => i.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasOne(i => i.Part)
            .WithMany()
            .HasForeignKey(i => i.PartId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasOne(i => i.AssignmentCode)
            .WithMany()
            .HasForeignKey(i => i.AssignmentCodeId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasOne(i => i.ProductionOrder)
            .WithMany()
            .HasForeignKey(i => i.ProductionOrderId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasIndex(i => i.AnchorSeedRowId)
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL AND \"AnchorSeedRowId\" IS NOT NULL");
        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasIndex(i => new { i.LongTermAnchorSeedId, i.ProcessGroupId, i.PartId })
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LongTermAnchorSeedItem>()
            .HasMany(i => i.Logs)
            .WithOne(l => l.LongTermAnchorSeedItem)
            .HasForeignKey(l => l.LongTermAnchorSeedItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LongTermAnchorSeedItemLog>()
            .HasOne(l => l.AcceptanceReport)
            .WithMany(a => a.LongTermAnchorSeedItemLogs)
            .HasForeignKey(l => l.AcceptanceReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LongTermAnchorSeedItemLog>()
            .HasIndex(l => new { l.LongTermAnchorSeedItemId, l.AcceptanceReportId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LongTermAnchorSeedItemLog>()
            .HasIndex(l => new { l.AcceptanceReportId, l.PeriodStartMonth })
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LongTermAnchorSeedProcessGroupMetric>()
            .HasOne(m => m.ProcessGroup)
            .WithMany()
            .HasForeignKey(m => m.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LongTermAnchorSeedProcessGroupMetric>()
            .HasIndex(m => new { m.LongTermAnchorSeedId, m.ProcessGroupId })
            .IsUnique()
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<ProductionOutputProcessGroup>()
            .HasOne(g => g.ProcessGroup)
            .WithMany()
            .HasForeignKey(g => g.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductionOutputProcessGroup>()
            .HasMany(g => g.ProductionOutputProducts)
            .WithOne(p => p.ProductionOutputProcessGroup)
            .HasForeignKey(p => p.ProductionOutputProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductionOutputProcessGroup>()
            .Property(g => g.PlanProductionMeters)
            .HasDefaultValue(0d);

        modelBuilder.Entity<ProductionOutputProduct>()
            .Property(p => p.ActualAshContent)
            .HasDefaultValue(0d);

        modelBuilder.Entity<ProductionOutputProduct>()
            .HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductionOutput table
        modelBuilder.Entity<AcceptanceReport>(entity =>
        {
            // 1. Cấu hình Quan hệ 1-1
            entity.HasOne(a => a.ProductionOutput)
                  .WithOne(p => p.AcceptanceReport)
                  .HasForeignKey<AcceptanceReport>(a => a.ProductionOutputId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.ActualElectricityCost)
              .WithOne(p => p.AcceptanceReport)
              .HasForeignKey<ActualElectricityCost>(a => a.AcceptanceReportId)
              .OnDelete(DeleteBehavior.Cascade);

            // 2. Cấu hình Index có điều kiện (Partial Index)
            // Phải ghi đè Index mặc định mà EF tự tạo cho Foreign Key
            entity.HasIndex(a => a.ProductionOutputId)
                  .IsUnique()
                  .HasFilter("\"DeletedOn\" IS NULL");
        });

        // AcceptanceReport table
        modelBuilder.Entity<AcceptanceReport>()
            .HasMany(a => a.AcceptanceReportItems)
            .WithOne(i => i.AcceptanceReport)
            .HasForeignKey(i => i.AcceptanceReportId)
            .OnDelete(DeleteBehavior.Cascade);

        // AcceptanceReportItem table
        modelBuilder.Entity<AcceptanceReportItem>()
            .Property(i => i.SortOrder)
            .HasDefaultValue(0);
        modelBuilder.Entity<AcceptanceReportItem>()
            .Property(i => i.IsLongTermTracking)
            .HasDefaultValue(false);
        modelBuilder.Entity<AcceptanceReportItem>()
            .Property(i => i.DocumentNumber)
            .HasMaxLength(255);
        modelBuilder.Entity<AcceptanceReportItem>()
            .Property(i => i.PostingDate)
            .HasColumnType("date");
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasIndex(i => new { i.AcceptanceReportId, i.SortOrder })
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasOne(i => i.ProcessGroup)
            .WithMany()
            .HasForeignKey(i => i.ProcessGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasOne(i => i.Part)
            .WithMany()
            .HasForeignKey(i => i.PartId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasOne(i => i.Material)
            .WithMany(i => i.AcceptanceReportItems)
            .HasForeignKey(i => i.MaterialId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasMany(i => i.ShippedDetails)
            .WithOne(i => i.AcceptanceReportItem)
            .HasForeignKey(i => i.AcceptanceReportItemId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasMany(i => i.IssuedDetails)
            .WithOne(i => i.AcceptanceReportItem)
            .HasForeignKey(i => i.AcceptanceReportItemId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasMany(i => i.QuotaBasedMaterialQuantities)
            .WithOne(i => i.AcceptanceReportItem)
            .HasForeignKey(i => i.AcceptanceReportItemId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcceptanceReportItem>()
            .HasMany(i => i.CategoryAllocations)
            .WithOne(i => i.AcceptanceReportItem)
            .HasForeignKey(i => i.AcceptanceReportItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptanceReportItemCategoryAllocation>()
            .HasOne(i => i.ProcessGroup)
            .WithMany()
            .HasForeignKey(i => i.ProcessGroupId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcceptanceReportItemCategoryAllocation>()
            .HasMany(i => i.Equipments)
            .WithOne(i => i.AcceptanceReportItemCategoryAllocation)
            .HasForeignKey(i => i.AcceptanceReportItemCategoryAllocationId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AcceptanceReportItemCategoryAllocation>()
            .HasMany(i => i.AcceptanceReportItemLogs)
            .WithOne(i => i.AcceptanceReportItemCategoryAllocation)
            .HasForeignKey(i => i.AcceptanceReportItemCategoryAllocationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptanceReportItemCategoryAllocationEquipment>()
            .HasOne(i => i.Equipment)
            .WithMany()
            .HasForeignKey(i => i.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptanceReportItemCategoryAllocation>()
            .HasIndex(i => new { i.AcceptanceReportItemId, i.ProcessGroupId })
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<AcceptanceReportItemCategoryAllocationEquipment>()
            .HasIndex(i => new { i.AcceptanceReportItemCategoryAllocationId, i.EquipmentId })
            .HasFilter("\"DeletedOn\" IS NULL");

        // AcceptanceReportItemLog table
        modelBuilder.Entity<AcceptanceReportItemLog>()
            .HasOne(l => l.AcceptanceReportItem)
            .WithMany(l => l.AcceptanceReportItemLogs)
            .HasForeignKey(l => l.AcceptanceReportItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptanceReportItemLog>()
            .HasOne(l => l.AcceptanceReport)
            .WithMany(l => l.AcceptanceReportItemLogs)
            .HasForeignKey(l => l.AcceptanceReportId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AcceptanceReportItemLog>()
            .HasIndex(l => new { l.AcceptanceReportItemId, l.AcceptanceReportId })
            .HasFilter("\"DeletedOn\" IS NULL");

        modelBuilder.Entity<LumpSumQuarterCustomCost>()
            .HasOne(l => l.ProcessGroup)
            .WithMany()
            .HasForeignKey(l => l.ProcessGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<LumpSumQuarterCustomCost>()
            .HasIndex(l => new { l.Year, l.Month, l.ProcessGroupId })
            .HasFilter("\"DeletedOn\" IS NULL");

        // ActualElectricityCost table
        modelBuilder.Entity<ActualElectricityCost>()
            .HasIndex(l => l.AcceptanceReportId)
            .HasFilter("\"DeletedOn\" IS NULL");
        modelBuilder.Entity<ActualElectricityCost>()
            .HasMany(l => l.ActualEletricityEquipment)
            .WithOne(l => l.ActualElectricityCost)
            .HasForeignKey(l => l.ActualElectricityCostId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        base.OnModelCreating(modelBuilder);
    }
}

