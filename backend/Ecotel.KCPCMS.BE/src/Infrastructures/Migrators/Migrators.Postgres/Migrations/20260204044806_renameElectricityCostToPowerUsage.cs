using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class renameElectricityCostToPowerUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ElectricityCost",
                schema: "Pricing",
                table: "ActualEquipmentElectricityCost",
                newName: "PowerUsage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PowerUsage",
                schema: "Pricing",
                table: "ActualEquipmentElectricityCost",
                newName: "ElectricityCost");
        }
    }
}
