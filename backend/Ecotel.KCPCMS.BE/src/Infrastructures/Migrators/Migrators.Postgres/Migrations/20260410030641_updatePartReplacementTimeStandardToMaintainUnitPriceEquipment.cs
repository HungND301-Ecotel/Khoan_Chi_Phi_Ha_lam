using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updatePartReplacementTimeStandardToMaintainUnitPriceEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Xóa Foreign Key cũ trỏ tới bảng Part
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            // 2. Thêm cột mới MaintainUnitPriceEquipmentId (Tạm thời cho phép NULL)
            migrationBuilder.AddColumn<Guid>(
                name: "MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            // 3. Thêm cột ReplacementTimeStandard vào bảng Pricing
            migrationBuilder.AddColumn<decimal>(
                name: "ReplacementTimeStandard",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // 4. LOGIC QUAN TRỌNG: Cứu dữ liệu cũ
            // Tìm ID định mức phù hợp dựa trên PartId cũ đã lưu trong AcceptanceReportItem
            migrationBuilder.Sql(@"
                UPDATE ""Production"".""AcceptanceReportItem"" ari
                SET ""MaintainUnitPriceEquipmentId"" = (
                    SELECT mue.""Id"" 
                    FROM ""Pricing"".""MaintainUnitPriceEquipment"" mue 
                    WHERE mue.""PartId"" = ari.""PartId""
                    LIMIT 1
                )
                WHERE ari.""PartId"" IS NOT NULL;
            ");

            // 5. Tạo Index cho cột mới
            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaintainUnitPriceEquipmentId");

            // 6. Tạo Foreign Key mới trỏ sang bảng định mức
            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaintainUnitPriceEquipmentId",
                principalSchema: "Pricing",
                principalTable: "MaintainUnitPriceEquipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // 7. Dọn dẹp: Xóa cột cũ và index cũ
            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ReplacementTimeStandard",
                schema: "Index",
                table: "Part");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phục hồi lại trạng thái cũ
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_MaintainUnitPriceEquipment_MaintainUni~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "ReplacementTimeStandard",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment");

            migrationBuilder.AddColumn<Guid>(
                name: "PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            // Có thể dùng SQL ngược lại nếu cần giữ data khi Down
            migrationBuilder.Sql(@"
                UPDATE ""Production"".""AcceptanceReportItem"" ari
                SET ""PartId"" = (
                    SELECT mue.""PartId"" 
                    FROM ""Pricing"".""MaintainUnitPriceEquipment"" mue 
                    WHERE mue.""Id"" = ari.""MaintainUnitPriceEquipmentId""
                    LIMIT 1
                )
            ");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId");

            migrationBuilder.AddColumn<decimal>(
                name: "ReplacementTimeStandard",
                schema: "Index",
                table: "Part",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropColumn(
                name: "MaintainUnitPriceEquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");
        }
    }
}