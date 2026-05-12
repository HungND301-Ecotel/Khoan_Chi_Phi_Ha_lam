using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateLongTermAnchorSeedItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LongTermAnchorSeedProcessGroupMetric",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LongTermAnchorSeedId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedOutput = table.Column<double>(type: "double precision", nullable: false),
                    StandardOutput = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongTermAnchorSeedProcessGroupMetric", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedProcessGroupMetric_LongTermAnchorSeed_Lon~",
                        column: x => x.LongTermAnchorSeedId,
                        principalSchema: "Production",
                        principalTable: "LongTermAnchorSeed",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LongTermAnchorSeedProcessGroupMetric_ProcessGroup_ProcessGr~",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedProcessGroupMetric_LongTermAnchorSeedId_P~",
                schema: "Production",
                table: "LongTermAnchorSeedProcessGroupMetric",
                columns: new[] { "LongTermAnchorSeedId", "ProcessGroupId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedProcessGroupMetric_ProcessGroupId",
                schema: "Production",
                table: "LongTermAnchorSeedProcessGroupMetric",
                column: "ProcessGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LongTermAnchorSeedProcessGroupMetric",
                schema: "Production");
        }
    }
}
