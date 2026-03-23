using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAcceptanceReportQuotaBasedMaterialQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuotaBasedMaterialQuantity",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.AddColumn<int>(
                name: "OtherMaterialDetail",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemQuotaBasedMaterialQuantity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceReportItemQuotaBasedMaterialQuantity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemQuotaBasedMaterialQuantity_AcceptanceRe~",
                        column: x => x.AcceptanceReportItemId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemQuotaBasedMaterialQuantity_AcceptanceRe~",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity",
                column: "AcceptanceReportItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptanceReportItemQuotaBasedMaterialQuantity");

            migrationBuilder.DropColumn(
                name: "OtherMaterialDetail",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.AddColumn<double>(
                name: "QuotaBasedMaterialQuantity",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
