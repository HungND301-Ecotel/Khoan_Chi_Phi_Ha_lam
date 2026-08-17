using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportPlanLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportPlanLine",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScenarioType = table.Column<int>(type: "integer", nullable: false),
                    OutputType = table.Column<int>(type: "integer", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentQuality = table.Column<string>(type: "text", nullable: true),
                    TransportRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    RouteDepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    HaulDistanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportPlanLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_AssignmentCode_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "Index",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_Department_RouteDepartmentId",
                        column: x => x.RouteDepartmentId,
                        principalSchema: "Index",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_HaulDistance_HaulDistanceId",
                        column: x => x.HaulDistanceId,
                        principalSchema: "Index",
                        principalTable: "HaulDistance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_ProductionProcess_ProductionProcessId",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_TransportRoute_TransportRouteId",
                        column: x => x.TransportRouteId,
                        principalSchema: "Index",
                        principalTable: "TransportRoute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportPlanLine_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedTransportCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportPlanLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportUnitPriceId = table.Column<Guid>(type: "uuid", nullable: true),
                    MechanizedTransportUnitPriceDetailId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedTransportCost", x => x.Id);
                    table.CheckConstraint("CK_PlannedTransportCost_ExactlyOneUnitPriceReference", "((\"TransportUnitPriceId\" IS NOT NULL AND \"MechanizedTransportUnitPriceDetailId\" IS NULL) OR (\"TransportUnitPriceId\" IS NULL AND \"MechanizedTransportUnitPriceDetailId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_PlannedTransportCost_MechanizedTransportUnitPriceDetail_Mec~",
                        column: x => x.MechanizedTransportUnitPriceDetailId,
                        principalSchema: "Pricing",
                        principalTable: "MechanizedTransportUnitPriceDetail",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedTransportCost_TransportPlanLine_TransportPlanLineId",
                        column: x => x.TransportPlanLineId,
                        principalSchema: "Pricing",
                        principalTable: "TransportPlanLine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedTransportCost_TransportUnitPrice_TransportUnitPriceId",
                        column: x => x.TransportUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "TransportUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedTransportCostAdjustmentFactor",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedTransportCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomValue = table.Column<double>(type: "double precision", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedTransportCostAdjustmentFactor", x => x.Id);
                    table.CheckConstraint("CK_PlannedTransportCostAdjustmentFactor_CustomOrReference", "\r\n                    (\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NOT NULL AND\r\n                            \"AdjustmentFactorId\" IS NULL AND\r\n                            \"CustomValue\" IS NULL\r\n                        )\r\n                        OR\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NULL AND\r\n                            \"AdjustmentFactorId\" IS NOT NULL AND\r\n                            \"CustomValue\" IS NOT NULL\r\n                        )\r\n                    )\r\n                ");
                    table.ForeignKey(
                        name: "FK_PlannedTransportCostAdjustmentFactor_AdjustmentFactorDescri~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedTransportCostAdjustmentFactor_AdjustmentFactor_Adjus~",
                        column: x => x.AdjustmentFactorId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlannedTransportCostAdjustmentFactor_PlannedTransportCost_P~",
                        column: x => x.PlannedTransportCostId,
                        principalSchema: "Pricing",
                        principalTable: "PlannedTransportCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCost_MechanizedTransportUnitPriceDetailId",
                schema: "Pricing",
                table: "PlannedTransportCost",
                column: "MechanizedTransportUnitPriceDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCost_TransportPlanLineId",
                schema: "Pricing",
                table: "PlannedTransportCost",
                column: "TransportPlanLineId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCost_TransportUnitPriceId",
                schema: "Pricing",
                table: "PlannedTransportCost",
                column: "TransportUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCostAdjustmentFactor_AdjustmentFactorDescri~",
                schema: "Index",
                table: "PlannedTransportCostAdjustmentFactor",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCostAdjustmentFactor_AdjustmentFactorId",
                schema: "Index",
                table: "PlannedTransportCostAdjustmentFactor",
                column: "AdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedTransportCostAdjustmentFactor_PlannedTransportCostId",
                schema: "Index",
                table: "PlannedTransportCostAdjustmentFactor",
                column: "PlannedTransportCostId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_DepartmentId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_EquipmentId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_HaulDistanceId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "HaulDistanceId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_ProductionProcessId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_RouteDepartmentId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "RouteDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_TransportRouteId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "TransportRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "UnitOfMeasureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlannedTransportCostAdjustmentFactor",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PlannedTransportCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "TransportPlanLine",
                schema: "Pricing");
        }
    }
}
