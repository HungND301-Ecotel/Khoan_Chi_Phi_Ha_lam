using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateMaintainElectricityTrimmingCoeffictientCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "TrimmingCoefficient",
                schema: "Pricing",
                table: "PlannedMaintainCost",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrimmingCoefficient",
                schema: "Pricing",
                table: "PlannedElectricityCost",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrimmingCoefficient",
                schema: "Pricing",
                table: "PlannedMaintainCost");

            migrationBuilder.DropColumn(
                name: "TrimmingCoefficient",
                schema: "Pricing",
                table: "PlannedElectricityCost");
        }
    }
}
