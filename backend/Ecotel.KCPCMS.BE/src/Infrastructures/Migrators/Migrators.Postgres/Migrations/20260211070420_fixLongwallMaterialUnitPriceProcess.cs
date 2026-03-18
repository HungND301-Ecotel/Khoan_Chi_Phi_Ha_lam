using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class fixLongwallMaterialUnitPriceProcess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1️⃣ Drop column không còn dùng
            migrationBuilder.DropColumn(
                name: "LongwallType",
                schema: "Pricing",
                table: "MaterialUnitPrice");


            // 2️⃣ Gán ProcessId hợp lệ cho các record NULL
            // lấy 1 ProductionProcess.Id bất kỳ tồn tại
            migrationBuilder.Sql("""
                UPDATE "Pricing"."MaterialUnitPrice"
                SET "ProcessId" = p."Id"
                FROM (
                    SELECT "Id"
                    FROM "Index"."ProductionProcess"
                    ORDER BY "Id"
                    LIMIT 1
                ) p
                WHERE "ProcessId" IS NULL;
            """);


            // 3️⃣ Chuyển ProcessId → NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "ProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);


            // 4️⃣ Thêm cột mới nếu bạn thật sự cần
            migrationBuilder.AddColumn<Guid>(
                name: "ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "ProductionProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_ProductionProcess_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "ProductionProcessId",
                principalSchema: "Index",
                principalTable: "ProductionProcess",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_ProductionProcess_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProductionProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "LongwallType",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "integer",
                nullable: true);
        }
    }
}
