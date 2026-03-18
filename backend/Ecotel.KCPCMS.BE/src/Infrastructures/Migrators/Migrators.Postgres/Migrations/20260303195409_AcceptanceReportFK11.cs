using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AcceptanceReportFK11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReport_ProductionOutputId",
                schema: "Production",
                table: "AcceptanceReport");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReport_ProductionOutputId",
                schema: "Production",
                table: "AcceptanceReport",
                column: "ProductionOutputId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReport_ProductionOutputId",
                schema: "Production",
                table: "AcceptanceReport");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReport_ProductionOutputId",
                schema: "Production",
                table: "AcceptanceReport",
                column: "ProductionOutputId");
        }
    }
}
