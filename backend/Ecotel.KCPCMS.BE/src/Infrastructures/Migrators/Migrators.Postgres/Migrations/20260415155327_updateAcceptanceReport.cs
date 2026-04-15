using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "PartId",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}
