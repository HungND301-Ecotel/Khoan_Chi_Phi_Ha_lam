using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addFixedKeyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FixedKey",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedKey", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "Index",
                table: "FixedKey",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "DeletedBy", "DeletedOn", "Key", "LastModifiedBy", "LastModifiedOn", "Name", "Type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "DL", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Đào lò", 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "LC", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lò chợ", 2 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "XL", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Xén lò", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessGroup_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
                column: "FixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedKey_Key",
                schema: "Index",
                table: "FixedKey",
                column: "Key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessGroup_FixedKey_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
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
                name: "FK_ProcessGroup_FixedKey_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropTable(
                name: "FixedKey",
                schema: "Index");

            migrationBuilder.DropIndex(
                name: "IX_ProcessGroup_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");
        }
    }
}
