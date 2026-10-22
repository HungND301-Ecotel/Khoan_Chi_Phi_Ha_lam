using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleTypeToTransportLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                schema: "Pricing",
                table: "TransportPlanLine",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VehicleType",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleType",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                schema: "Production",
                table: "ProductionOutputTransportLine");
        }
    }
}
