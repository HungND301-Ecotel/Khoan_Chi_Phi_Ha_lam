using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAcceptanceReportItemQuantityType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IssuedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ShippedQuantity",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemIssuedDetail",
                schema: "Production",
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
                    table.PrimaryKey("PK_AcceptanceReportItemIssuedDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemIssuedDetail_AcceptanceReportItem_Accep~",
                        column: x => x.AcceptanceReportItemId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemShippedDetail",
                schema: "Production",
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
                    table.PrimaryKey("PK_AcceptanceReportItemShippedDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemShippedDetail_AcceptanceReportItem_Acce~",
                        column: x => x.AcceptanceReportItemId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemIssuedDetail_AcceptanceReportItemId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail",
                column: "AcceptanceReportItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemShippedDetail_AcceptanceReportItemId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail",
                column: "AcceptanceReportItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptanceReportItemIssuedDetail",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "AcceptanceReportItemShippedDetail",
                schema: "Production");

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
    }
}
