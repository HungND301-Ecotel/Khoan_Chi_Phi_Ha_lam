using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addVoucherFieldsToAcceptanceReportItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PostingDate",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "PostingDate",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}
