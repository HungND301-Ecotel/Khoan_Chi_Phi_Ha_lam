using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class removeAccepteanceReportPreviosId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemLog_PreviousLog~",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemLog_PreviousLogId",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_PreviousLogId",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "PreviousLogId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemLog_PreviousLog~",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "PreviousLogId",
                principalSchema: "Production",
                principalTable: "AcceptanceReportItemLog",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemLog_PreviousLog~",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemLog_PreviousLogId",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_PreviousLogId",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "PreviousLogId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemLog_PreviousLog~",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "PreviousLogId",
                principalSchema: "Production",
                principalTable: "AcceptanceReportItemLog",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
