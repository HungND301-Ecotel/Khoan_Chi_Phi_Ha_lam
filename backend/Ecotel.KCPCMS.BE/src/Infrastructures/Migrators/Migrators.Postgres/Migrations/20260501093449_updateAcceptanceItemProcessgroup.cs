using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceItemProcessgroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AcceptanceReportItemCategoryAllocationId",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemCategoryAllocation",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_AcceptanceReportItemCategoryAllocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemCategoryAllocation_AcceptanceReportItem~",
                        column: x => x.AcceptanceReportItemId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemCategoryAllocation_ProcessGroup_Process~",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcceptanceReportItemCategoryAllocationEquipment",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AcceptanceReportItemCategoryAllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcceptanceReportItemCategoryAllocationEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemCategoryAllocationEquipment_AcceptanceR~",
                        column: x => x.AcceptanceReportItemCategoryAllocationId,
                        principalSchema: "Production",
                        principalTable: "AcceptanceReportItemCategoryAllocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AcceptanceReportItemCategoryAllocationEquipment_Equipment_E~",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemLog_AcceptanceReportItemCategoryAllocat~",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "AcceptanceReportItemCategoryAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemCategoryAllocation_AcceptanceReportItem~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocation",
                columns: new[] { "AcceptanceReportItemId", "ProcessGroupId" },
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemCategoryAllocation_ProcessGroupId",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocation",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemCategoryAllocationEquipment_AcceptanceR~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment",
                columns: new[] { "AcceptanceReportItemCategoryAllocationId", "EquipmentId" },
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemCategoryAllocationEquipment_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemCategoryAllocat~",
                schema: "Production",
                table: "AcceptanceReportItemLog",
                column: "AcceptanceReportItemCategoryAllocationId",
                principalSchema: "Production",
                principalTable: "AcceptanceReportItemCategoryAllocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemLog_AcceptanceReportItemCategoryAllocat~",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.DropTable(
                name: "AcceptanceReportItemCategoryAllocationEquipment",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "AcceptanceReportItemCategoryAllocation",
                schema: "Production");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemLog_AcceptanceReportItemCategoryAllocat~",
                schema: "Production",
                table: "AcceptanceReportItemLog");

            migrationBuilder.DropColumn(
                name: "AcceptanceReportItemCategoryAllocationId",
                schema: "Production",
                table: "AcceptanceReportItemLog");
        }
    }
}
