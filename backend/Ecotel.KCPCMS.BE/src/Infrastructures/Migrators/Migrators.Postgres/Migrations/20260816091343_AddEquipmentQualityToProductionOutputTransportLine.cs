using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentQualityToProductionOutputTransportLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EquipmentQuality",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentQuality",
                schema: "Production",
                table: "ProductionOutputTransportLine");
        }
    }
}
