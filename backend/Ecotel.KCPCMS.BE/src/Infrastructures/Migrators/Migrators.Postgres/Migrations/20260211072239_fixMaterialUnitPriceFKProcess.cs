using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class fixMaterialUnitPriceFKProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_ProductionProcess_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "ProductionProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_ProductionProcess_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "ProductionProcessId",
                principalSchema: "Index",
                principalTable: "ProductionProcess",
                principalColumn: "Id");
        }
    }
}
