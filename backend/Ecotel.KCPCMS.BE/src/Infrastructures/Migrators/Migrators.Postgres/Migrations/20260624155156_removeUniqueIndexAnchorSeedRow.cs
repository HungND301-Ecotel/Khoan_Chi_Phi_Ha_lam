using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class removeUniqueIndexAnchorSeedRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LongTermAnchorSeedItem_LongTermAnchorSeedId_ProcessGroupId_~",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_LongTermAnchorSeedId_ProcessGroupId_~",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                columns: new[] { "LongTermAnchorSeedId", "ProcessGroupId", "PartId" },
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LongTermAnchorSeedItem_LongTermAnchorSeedId_ProcessGroupId_~",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_LongTermAnchorSeedId_ProcessGroupId_~",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                columns: new[] { "LongTermAnchorSeedId", "ProcessGroupId", "PartId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
