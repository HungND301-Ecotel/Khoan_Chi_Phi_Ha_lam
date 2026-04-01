using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlannedMaterialCostStoneClampRatio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoneClampRatioReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_StoneClampRatioReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "StoneClampRatioReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMaterialCost_StoneClampRatio_StoneClampRatioReferenc~",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "StoneClampRatioReferenceId",
                principalSchema: "Index",
                principalTable: "StoneClampRatio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMaterialCost_StoneClampRatio_StoneClampRatioReferenc~",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMaterialCost_StoneClampRatioReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropColumn(
                name: "StoneClampRatioReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost");
        }
    }
}
