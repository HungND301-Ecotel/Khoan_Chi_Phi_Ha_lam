using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddLowValuePerishableSupplyInclusionToPlannedTransportCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowValuePerishableSupplyInclusion",
                schema: "Pricing",
                table: "PlannedTransportCost",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LowValuePerishableSupplyInclusion",
                schema: "Pricing",
                table: "PlannedTransportCost");
        }
    }
}
