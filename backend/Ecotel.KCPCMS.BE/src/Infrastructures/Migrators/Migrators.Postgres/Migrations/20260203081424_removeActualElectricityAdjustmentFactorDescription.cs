using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class removeActualElectricityAdjustmentFactorDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActualElectricityCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ActualElectricityCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.CreateTable(
                name: "ActualEquipmentElectricityCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualElectricityCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectricityUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ElectricityCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActualEquipmentElectricityCost",
                schema: "Pricing");

            migrationBuilder.CreateTable(
                name: "ActualElectricityCostAdjustmentFactor",
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
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualElectricityCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactor_ActualElectricityCost~",
                        column: x => x.ActualElectricityCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualElectricityCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactor_ElectricityUnitPriceE~",
                        column: x => x.ElectricityUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ElectricityUnitPriceEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualElectricityCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualElectricityCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ActualElectricityCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactorDescription_ActualElec~",
                        column: x => x.ActualElectricityCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "ActualElectricityCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactorDescription_Adjustment~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactor_ActualElectricityCost~",
                schema: "Pricing",
                table: "ActualElectricityCostAdjustmentFactor",
                column: "ActualElectricityCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactor_ElectricityUnitPriceId",
                schema: "Pricing",
                table: "ActualElectricityCostAdjustmentFactor",
                column: "ElectricityUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactorDescription_ActualElec~",
                schema: "Index",
                table: "ActualElectricityCostAdjustmentFactorDescription",
                column: "ActualElectricityCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactorDescription_Adjustment~",
                schema: "Index",
                table: "ActualElectricityCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");
        }
    }
}
