using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addTunnelSupportAndDrillingMaterialUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TunnelSupportAndDrillingMaterialUnitPrice_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TunnelSupportAndDrillingMaterialUnitPrice_PassportId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_TunnelSupportAndDrillingMaterialUnitPric~1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelSupportAndDrillingMaterialUnitPrice_PassportId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_TunnelSupportAndDrillingMaterialUnitPrice~",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelSupportAndDrillingMaterialUnitPrice_HardnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_TunnelSupportAndDrillingMaterial~",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelSupportAndDrillingMaterialUnitPrice_HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Passport_TunnelSupportAndDrillingMaterial~",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelSupportAndDrillingMaterialUnitPrice_PassportId",
                principalSchema: "Index",
                principalTable: "Passport",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_TunnelSupportAndDrillingMaterial~",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Passport_TunnelSupportAndDrillingMaterial~",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_TunnelSupportAndDrillingMaterialUnitPric~1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_TunnelSupportAndDrillingMaterialUnitPrice~",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "TunnelSupportAndDrillingMaterialUnitPrice_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "TunnelSupportAndDrillingMaterialUnitPrice_PassportId",
                schema: "Pricing",
                table: "MaterialUnitPrice");
        }
    }
}
