using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransportUnitPriceEquipmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportUnitPrice_Material_MaterialId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_TransportUnitPrice_MaterialId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                newName: "IX_TransportUnitPrice_EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportUnitPrice_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportUnitPrice_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_TransportUnitPrice_EquipmentId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                newName: "IX_TransportUnitPrice_MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportUnitPrice_Material_MaterialId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "MaterialId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
