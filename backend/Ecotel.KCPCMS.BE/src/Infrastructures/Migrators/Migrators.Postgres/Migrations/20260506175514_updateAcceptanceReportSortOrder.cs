using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReportSortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_AcceptanceReportId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_AcceptanceReportId_SortOrder",
                schema: "Production",
                table: "AcceptanceReportItem",
                columns: new[] { "AcceptanceReportId", "SortOrder" },
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_AcceptanceReportId_SortOrder",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_AcceptanceReportId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AcceptanceReportId");
        }
    }
}
