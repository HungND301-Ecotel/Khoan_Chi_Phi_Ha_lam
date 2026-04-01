using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePlannedMaterialMaterialId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "MaterialReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMaterialCost_Material_MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "MaterialReferenceId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMaterialCost_Material_MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMaterialCost_MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropColumn(
                name: "MaterialReferenceId",
                schema: "Pricing",
                table: "PlannedMaterialCost");
        }
    }
}
