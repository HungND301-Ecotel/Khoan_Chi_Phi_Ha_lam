using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateProductionOutputPlanOutput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlannedOutput",
                schema: "Production",
                table: "ProductionOutputProduct");

            migrationBuilder.AddColumn<double>(
                name: "PlanProductionMeters",
                schema: "Production",
                table: "ProductionOutputProcessGroup",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanProductionMeters",
                schema: "Production",
                table: "ProductionOutputProcessGroup");

            migrationBuilder.AddColumn<double>(
                name: "PlannedOutput",
                schema: "Production",
                table: "ProductionOutputProduct",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
