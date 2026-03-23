using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReportItemItemType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_ProductionOrder_ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "ProductionOrderId",
                principalSchema: "Index",
                principalTable: "ProductionOrder",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_ProductionOrder_ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ItemType",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}
