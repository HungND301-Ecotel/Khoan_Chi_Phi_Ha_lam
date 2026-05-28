using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addMaterialRowsToMaterialUnitPriceAssignmentCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Assignm~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Norm",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Assignm~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                columns: new[] { "MaterialUnitPriceId", "AssignmentCodeId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPriceAssignmentCode_Material_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                column: "MaterialId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPriceAssignmentCode_Material_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Assignm~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.DropColumn(
                name: "Norm",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Assignm~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                columns: new[] { "MaterialUnitPriceId", "AssignmentCodeId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
