using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addLongTermAnchorSeedItemLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LongTermAnchorSeedItemLog",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LongTermAnchorSeedItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    PendingValueStartPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IssuedQuantity = table.Column<double>(type: "double precision", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OriginAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalValueToAccount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageTime = table.Column<double>(type: "double precision", nullable: false),
                    AllocatedTime = table.Column<double>(type: "double precision", nullable: false),
                    RemainingTime = table.Column<double>(type: "double precision", nullable: false),
                    ActualOutput = table.Column<double>(type: "double precision", nullable: false),
                    PlannedOutput = table.Column<double>(type: "double precision", nullable: false),
                    StandardOutput = table.Column<double>(type: "double precision", nullable: false),
                    ValueByStandard = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocationRatio = table.Column<double>(type: "double precision", nullable: false),
                    AccountedValueThisPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PendingValueEndPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTermAnchorSeedItemLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedItemLog_AcceptanceReport_AcceptanceReport~",
                        column: x => x.AcceptanceReportId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedItemLog_LongTermAnchorSeedItem_LongTermAn~",
                        column: x => x.LongTermAnchorSeedItemId,
                        principalSchema: "Production",
                        principalTable: "LongTermAnchorSeedItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItemLog_AcceptanceReportId_PeriodStartMon~",
                schema: "Production",
                table: "LongTermAnchorSeedItemLog",
                columns: new[] { "AcceptanceReportId", "PeriodStartMonth" },
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItemLog_LongTermAnchorSeedItemId_Acceptan~",
                schema: "Production",
                table: "LongTermAnchorSeedItemLog",
                columns: new[] { "LongTermAnchorSeedItemId", "AcceptanceReportId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LongTermAnchorSeedItemLog",
                schema: "Production");
        }
    }
}
