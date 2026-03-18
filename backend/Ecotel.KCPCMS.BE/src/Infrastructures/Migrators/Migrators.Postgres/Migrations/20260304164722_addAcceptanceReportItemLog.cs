using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addAcceptanceReportItemLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemLog",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    PeriodStartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    PendingValueStartPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IssuedQuantity = table.Column<double>(type: "double precision", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalValueToAccount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageTime = table.Column<double>(type: "double precision", nullable: false),
                    AllocatedTime = table.Column<double>(type: "double precision", nullable: false),
                    RemainingTime = table.Column<double>(type: "double precision", nullable: false),
                    ActualOutput = table.Column<double>(type: "double precision", nullable: false),
                    StandardOutput = table.Column<double>(type: "double precision", nullable: false),
                    ValueByStandard = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationRatio = table.Column<double>(type: "double precision", nullable: false),
                    AccountedValueThisPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PendingValueEndPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceReportItemLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemLog_AcceptanceReportItemLog_PreviousLog~",
                        column: x => x.PreviousLogId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItemLog",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemLog_AcceptanceReportItem_AcceptanceRepo~",
                        column: x => x.AcceptanceReportItemId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemLog_AcceptanceReport_AcceptanceReportId",
                        column: x => x.AcceptanceReportId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_AcceptanceReportId",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "AcceptanceReportId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_AcceptanceReportItemId_AcceptanceRe~",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                columns: new[] { "AcceptanceReportItemId", "AcceptanceReportId" },
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_PreviousLogId",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "PreviousLogId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcceptanceReportItemLog",
                schema: "Production");
        }
    }
}
