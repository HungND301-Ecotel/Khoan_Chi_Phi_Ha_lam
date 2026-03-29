using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReportItemEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Equipment_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Equipment_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}
