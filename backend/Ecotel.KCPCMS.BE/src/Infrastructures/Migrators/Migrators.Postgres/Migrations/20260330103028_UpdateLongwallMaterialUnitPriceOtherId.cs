using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLongwallMaterialUnitPriceOtherId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AddColumn<bool>(
                name: "IsLongwallMaterialUnitPriceCGH",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TunnelExcavationMaterialUnitPrice_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "PowerId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "StoneClampRatioId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_TunnelExcavationMaterialUnitPrice_Hardnes~",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelExcavationMaterialUnitPrice_HardnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_TunnelExcavationMaterialUnitPric~",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TunnelExcavationMaterialUnitPrice_HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Power_PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "PowerId",
                principalSchema: "Index",
                principalTable: "Power",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_StoneClampRatio_StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "StoneClampRatioId",
                principalSchema: "Index",
                principalTable: "StoneClampRatio",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_TunnelExcavationMaterialUnitPric~",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Power_PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_StoneClampRatio_StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_TunnelExcavationMaterialUnitPrice_Hardnes~",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "IsLongwallMaterialUnitPriceCGH",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "PowerId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "StoneClampRatioId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "TunnelExcavationMaterialUnitPrice_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Hardness_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
