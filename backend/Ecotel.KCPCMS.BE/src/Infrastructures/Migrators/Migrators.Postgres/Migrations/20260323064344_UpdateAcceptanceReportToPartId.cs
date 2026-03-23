using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAcceptanceReportToPartId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.RenameColumn(
                name: "MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                newName: "PartId");

            migrationBuilder.RenameIndex(
                name: "IX_AcceptanceReportItem_MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                newName: "IX_AcceptanceReportItem_PartId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.RenameColumn(
                name: "PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                newName: "MaintainUnitPriceEquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_AcceptanceReportItem_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                newName: "IX_AcceptanceReportItem_MaintainUnitPriceEquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaintainUnitPriceEquipmentId",
                principalSchema: "Pricing",
                principalTable: "MaintainUnitPriceEquipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
