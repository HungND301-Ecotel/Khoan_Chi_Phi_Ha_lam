using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addMaterialUnitPriceAssignmentCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPrice",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                newName: "OtherMaterialvalue");

            migrationBuilder.CreateTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPrice = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialUnitPriceAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_AssignmentCode_AssignmentCo~",
                        column: x => x.AssignmentCodeId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_MaterialUnitPrice_MaterialU~",
                        column: x => x.MaterialUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaterialUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_AssignmentCodeId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                column: "AssignmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Assignm~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                columns: new[] { "MaterialUnitPriceId", "AssignmentCodeId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing");

            migrationBuilder.RenameColumn(
                name: "OtherMaterialvalue",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                newName: "TotalPrice");
        }
    }
}
