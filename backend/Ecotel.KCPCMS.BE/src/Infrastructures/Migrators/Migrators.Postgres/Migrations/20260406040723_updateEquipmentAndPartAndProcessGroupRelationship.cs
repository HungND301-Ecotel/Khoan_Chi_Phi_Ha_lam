using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateEquipmentAndPartAndProcessGroupRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EquipmentProcessGroup",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentProcessGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentProcessGroup_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentProcessGroup_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartProcessGroup",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartProcessGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartProcessGroup_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartProcessGroup_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentProcessGroup_EquipmentId_ProcessGroupId",
                schema: "Index",
                table: "EquipmentProcessGroup",
                columns: new[] { "EquipmentId", "ProcessGroupId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentProcessGroup_ProcessGroupId",
                schema: "Index",
                table: "EquipmentProcessGroup",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessGroup_PartId_ProcessGroupId",
                schema: "Index",
                table: "PartProcessGroup",
                columns: new[] { "PartId", "ProcessGroupId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessGroup_ProcessGroupId",
                schema: "Index",
                table: "PartProcessGroup",
                column: "ProcessGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EquipmentProcessGroup",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PartProcessGroup",
                schema: "Index");
        }
    }
}
