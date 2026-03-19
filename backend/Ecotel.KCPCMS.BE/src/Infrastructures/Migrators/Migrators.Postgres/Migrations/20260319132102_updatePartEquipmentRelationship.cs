using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updatePartEquipmentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Part_Equipment_EquipmentId",
                schema: "Index",
                table: "Part");

            migrationBuilder.DropIndex(
                name: "IX_Part_EquipmentId",
                schema: "Index",
                table: "Part");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                schema: "Index",
                table: "Part");

            migrationBuilder.CreateTable(
                name: "EquipmentPart",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentPart_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentPart_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart",
                columns: new[] { "EquipmentId", "PartId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_PartId",
                schema: "Index",
                table: "EquipmentPart",
                column: "PartId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentPart",
                schema: "Index");

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                schema: "Index",
                table: "Part",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Part_EquipmentId",
                schema: "Index",
                table: "Part",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Part_Equipment_EquipmentId",
                schema: "Index",
                table: "Part",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
