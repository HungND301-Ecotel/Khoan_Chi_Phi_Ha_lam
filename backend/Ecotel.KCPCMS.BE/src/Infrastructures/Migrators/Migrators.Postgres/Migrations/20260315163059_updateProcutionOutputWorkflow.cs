using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateProcutionOutputWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.AddColumn<double>(
                name: "ProductionMeters",
                schema: "Pricing",
                table: "ProductUnitPriceProductionOutput",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOutputProcessGroup",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StandardProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOutputProcessGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOutputProcessGroup_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOutputProcessGroup_ProductionOutput_ProductionOut~",
                        column: x => x.ProductionOutputId,
                        principalSchema: "Production",
                        principalTable: "ProductionOutput",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOutputProduct",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOutputProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOutputProduct", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOutputProduct_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Index",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOutputProduct_ProductionOutputProcessGroup_Produc~",
                        column: x => x.ProductionOutputProcessGroupId,
                        principalSchema: "Production",
                        principalTable: "ProductionOutputProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice",
                columns: new[] { "ProductId", "ScenarioType" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputProcessGroup_ProcessGroupId",
                schema: "Production",
                table: "ProductionOutputProcessGroup",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputProcessGroup_ProductionOutputId",
                schema: "Production",
                table: "ProductionOutputProcessGroup",
                column: "ProductionOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputProduct_ProductId",
                schema: "Production",
                table: "ProductionOutputProduct",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputProduct_ProductionOutputProcessGroupId",
                schema: "Production",
                table: "ProductionOutputProduct",
                column: "ProductionOutputProcessGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_ProcessGroup_ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "ProcessGroupId",
                principalSchema: "Index",
                principalTable: "ProcessGroup",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_ProcessGroup_ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropTable(
                name: "ProductionOutputProduct",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "ProductionOutputProcessGroup",
                schema: "Production");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ProductionMeters",
                schema: "Pricing",
                table: "ProductUnitPriceProductionOutput");

            migrationBuilder.DropColumn(
                name: "ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
