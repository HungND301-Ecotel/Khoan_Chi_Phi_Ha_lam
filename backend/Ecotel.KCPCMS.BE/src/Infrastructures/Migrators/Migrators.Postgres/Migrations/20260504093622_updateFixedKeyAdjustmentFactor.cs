using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateFixedKeyAdjustmentFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor",
                type: "uuid",
                nullable: true);

            migrationBuilder.InsertData(
                schema: "Index",
                table: "FixedKey",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "DeletedBy", "DeletedOn", "Key", "LastModifiedBy", "LastModifiedOn", "Name", "Type" },
                values: new object[,]
                {
                    { new Guid("44444444-4444-4444-4444-444444444444"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K1", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K1", 4 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K2", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K2", 5 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K3", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K3", 6 },
                    { new Guid("77777777-7777-7777-7777-777777777777"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K4", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K4", 7 },
                    { new Guid("88888888-8888-8888-8888-888888888888"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K5", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K5", 8 },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K6", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K6", 9 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K7", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K7", 10 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "K8", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Hệ số điều chỉnh K8", 11 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentFactor_FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor",
                column: "FixedKeyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdjustmentFactor_FixedKey_FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor",
                column: "FixedKeyId",
                principalSchema: "Index",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdjustmentFactor_FixedKey_FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor");

            migrationBuilder.DropIndex(
                name: "IX_AdjustmentFactor_FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor");

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("88888888-8888-8888-8888-888888888888"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("99999999-9999-9999-9999-999999999999"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                schema: "Index",
                table: "AdjustmentFactor");
        }
    }
}
