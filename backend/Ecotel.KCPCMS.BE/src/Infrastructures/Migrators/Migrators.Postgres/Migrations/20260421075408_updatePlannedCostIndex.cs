using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updatePlannedCostIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlannedMaterialCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMaintainCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedElectricityCost_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "OutputId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost",
                column: "OutputId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCost_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost",
                column: "OutputId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlannedMaterialCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMaintainCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedElectricityCost_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCost_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost",
                column: "OutputId",
                unique: true);
        }
    }
}
