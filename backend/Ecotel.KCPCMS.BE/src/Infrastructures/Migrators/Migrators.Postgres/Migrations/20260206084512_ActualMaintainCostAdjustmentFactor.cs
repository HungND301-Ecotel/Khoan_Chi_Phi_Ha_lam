using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class ActualMaintainCostAdjustmentFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "K6AdjustmentFactorValue",
                schema: "Pricing",
                table: "PlannedMaintainCostAdjustmentFactor",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "K6AdjustmentFactorValue",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "K6AdjustmentFactorValue",
                schema: "Pricing",
                table: "PlannedMaintainCostAdjustmentFactor");

            migrationBuilder.DropColumn(
                name: "K6AdjustmentFactorValue",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor");
        }
    }
}
