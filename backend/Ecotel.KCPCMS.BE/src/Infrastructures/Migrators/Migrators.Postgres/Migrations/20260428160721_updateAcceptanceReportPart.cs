using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAcceptanceReportPart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.AddColumn<double>(
                name: "UsageTime",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaintainUnitPriceEquipmentId",
                principalSchema: "Pricing",
                principalTable: "MaintainUnitPriceEquipment",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "UsageTime",
                schema: "Production",
                table: "AcceptanceReportItem");

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
