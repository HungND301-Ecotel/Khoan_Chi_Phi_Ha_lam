using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddRouteDepartmentToProductionOutputTransportLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "RouteDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputTransportLine_Department_RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "RouteDepartmentId",
                principalSchema: "Index",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutputTransportLine_Department_RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutputTransportLine_RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropColumn(
                name: "RouteDepartmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine");
        }
    }
}
