using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addLongTermAnchorSeedItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LongTermAnchorSeed",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTermAnchorSeed", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeed_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "Index",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LongTermAnchorSeedItem",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LongTermAnchorSeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    AnchorSeedRowId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IssuedQuantity = table.Column<double>(type: "double precision", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PendingValueStartPeriod = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageTime = table.Column<double>(type: "double precision", nullable: false),
                    AllocatedTime = table.Column<double>(type: "double precision", nullable: false),
                    AllocationRatio = table.Column<double>(type: "double precision", nullable: false),
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
                    table.PrimaryKey("PK_LongTermAnchorSeedItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedItem_LongTermAnchorSeed_LongTermAnchorSee~",
                        column: x => x.LongTermAnchorSeedId,
                        principalSchema: "Production",
                        principalTable: "LongTermAnchorSeed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedItem_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedItem_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeed_DepartmentId",
                schema: "Production",
                table: "LongTermAnchorSeed",
                column: "DepartmentId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_AnchorSeedRowId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "AnchorSeedRowId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL AND \"AnchorSeedRowId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_LongTermAnchorSeedId_ProcessGroupId_~",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                columns: new[] { "LongTermAnchorSeedId", "ProcessGroupId", "PartId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_PartId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_ProcessGroupId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "ProcessGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LongTermAnchorSeedItem",
                schema: "Production");

            migrationBuilder.DropTable(
                name: "LongTermAnchorSeed",
                schema: "Production");
        }
    }
}
