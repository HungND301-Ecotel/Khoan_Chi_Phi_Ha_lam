using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addSchemaProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Production");

            migrationBuilder.CreateTable(
                name: "ProductionOutput",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    StandardProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOutput", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AcceptanceReport",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceReport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReport_ProductionOutput_ProductionOutputId",
                        column: x => x.ProductionOutputId,
                        principalSchema: "Production",
                        principalTable: "ProductionOutput",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItem",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintainUnitPriceEquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialsIncludedInContractRevenue = table.Column<int>(type: "integer", nullable: false),
                    MaterialsIncludedInContractRevenueQuantity = table.Column<double>(type: "double precision", nullable: false),
                    AdditionalCost = table.Column<int>(type: "integer", nullable: false),
                    AdditionalCostQuantity = table.Column<double>(type: "double precision", nullable: false),
                    QuotaBasedMaterial = table.Column<int>(type: "integer", nullable: false),
                    QuotaBasedMaterialQuantity = table.Column<double>(type: "double precision", nullable: false),
                    Asset = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceReportItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItem_AcceptanceReport_AcceptanceReportId",
                        column: x => x.AcceptanceReportId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                        column: x => x.MaintainUnitPriceEquipmentId,
                        principalSchema: "Pricing",
                        principalTable: "MaintainUnitPriceEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItem_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReport_ProductionOutputId",
                schema: "Production",
                table: "AcceptanceReport",
                column: "ProductionOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_AcceptanceReportId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AcceptanceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaintainUnitPriceEquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_MaterialId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaterialId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptanceReportItem",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "AcceptanceReport",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "ProductionOutput",
                schema: "Production");
        }
    }
}
