using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class removeMaterialUnitPriceAssignmentCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing");

            migrationBuilder.DropColumn(
                name: "OtherMaterialValue",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AddColumn<double>(
                name: "TotalPrice",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalPrice",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AddColumn<double>(
                name: "OtherMaterialValue",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Quantity = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialUnitPriceAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_MaterialUnitPrice_MaterialU~",
                        column: x => x.MaterialUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaterialUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Materia~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                columns: new[] { "MaterialUnitPriceId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
