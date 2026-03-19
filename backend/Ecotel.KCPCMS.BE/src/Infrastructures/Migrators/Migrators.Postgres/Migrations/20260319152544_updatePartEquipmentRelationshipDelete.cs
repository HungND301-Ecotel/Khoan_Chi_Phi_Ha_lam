using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updatePartEquipmentRelationshipDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart",
                columns: new[] { "EquipmentId", "PartId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart",
                columns: new[] { "EquipmentId", "PartId" },
                unique: true);
        }
    }
}
