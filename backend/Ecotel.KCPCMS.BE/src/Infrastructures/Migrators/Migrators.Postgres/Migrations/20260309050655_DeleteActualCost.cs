using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class DeleteActualCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActualEquipmentElectricityCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaintainCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ActualMaterialCostAssignmentCode",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualElectricityCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaintainCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaterialCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaintainCost",
                schema: "Pricing");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductionOutputId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnitPrice_ProductionOutput_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductionOutputId",
                principalSchema: "Production",
                principalTable: "ProductionOutput",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnitPrice_ProductionOutput_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.CreateTable(
                name: "ActualElectricityCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualElectricityCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaterialCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaterialCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualEquipmentElectricityCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualElectricityCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectricityUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PowerUsage = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualEquipmentElectricityCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualEquipmentElectricityCost_ActualElectricityCost_Actual~",
                        column: x => x.ActualElectricityCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualElectricityCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualEquipmentElectricityCost_ElectricityUnitPriceEquipmen~",
                        column: x => x.ElectricityUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ElectricityUnitPriceEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCostAdjustmentFactor",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaintainCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintainUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    K6AdjustmentFactorValue = table.Column<double>(type: "double precision", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactor_ActualMaintainCost_Actua~",
                        column: x => x.ActualMaintainCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaintainCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactor_MaintainUnitPrice_Mainta~",
                        column: x => x.MaintainUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaintainUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaterialCostAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaterialCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaterialCostAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCostAssignmentCode_ActualMaterialCost_ActualM~",
                        column: x => x.ActualMaterialCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaterialCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCostAssignmentCode_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaintainCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactorDescription_ActualMaintai~",
                        column: x => x.ActualMaintainCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaintainCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactorDescription_AdjustmentFac~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCost_OutputId",
                schema: "Pricing",
                table: "ActualElectricityCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualElectricityCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualEquipmentElectricityCost_ActualElectricityCostId",
                schema: "Pricing",
                table: "ActualEquipmentElectricityCost",
                column: "ActualElectricityCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualEquipmentElectricityCost_ElectricityUnitPriceId",
                schema: "Pricing",
                table: "ActualEquipmentElectricityCost",
                column: "ElectricityUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCost_OutputId",
                schema: "Pricing",
                table: "ActualMaintainCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualMaintainCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactor_ActualMaintainCostId",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor",
                column: "ActualMaintainCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactor_MaintainUnitPriceId",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor",
                column: "MaintainUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactorDescription_ActualMaintai~",
                schema: "Index",
                table: "ActualMaintainCostAdjustmentFactorDescription",
                column: "ActualMaintainCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactorDescription_AdjustmentFac~",
                schema: "Index",
                table: "ActualMaintainCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCost_OutputId",
                schema: "Pricing",
                table: "ActualMaterialCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualMaterialCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCostAssignmentCode_ActualMaterialCostId",
                schema: "Pricing",
                table: "ActualMaterialCostAssignmentCode",
                column: "ActualMaterialCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCostAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "ActualMaterialCostAssignmentCode",
                column: "MaterialId");
        }
    }
}
