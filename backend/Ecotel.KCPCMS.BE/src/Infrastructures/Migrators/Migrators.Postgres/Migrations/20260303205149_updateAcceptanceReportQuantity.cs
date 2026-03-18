using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReportQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "IssuedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShippedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ShippedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}
