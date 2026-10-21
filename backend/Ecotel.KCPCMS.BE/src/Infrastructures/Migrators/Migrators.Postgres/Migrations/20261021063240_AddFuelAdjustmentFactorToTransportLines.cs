using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelAdjustmentFactorToTransportLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hệ số nhiên liệu VTCG — dòng cũ mặc định 1 (không điều chỉnh)
            migrationBuilder.AddColumn<double>(
                name: "FuelAdjustmentFactor",
                schema: "Pricing",
                table: "TransportPlanLine",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);

            migrationBuilder.AddColumn<double>(
                name: "FuelAdjustmentFactor",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "double precision",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelAdjustmentFactor",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropColumn(
                name: "FuelAdjustmentFactor",
                schema: "Production",
                table: "ProductionOutputTransportLine");
        }
    }
}
