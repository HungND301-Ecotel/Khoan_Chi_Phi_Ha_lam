using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class splitMaterialUnitPriceLongwallTunnel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "SupportStepId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "PassportId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "InsertItemId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaterialType",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "character varying(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId1");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId1");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId",
                principalSchema: "Index",
                principalTable: "CuttingThickness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId1",
                principalSchema: "Index",
                principalTable: "CuttingThickness",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId",
                principalSchema: "Index",
                principalTable: "LongwallParameters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId1",
                principalSchema: "Index",
                principalTable: "LongwallParameters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId",
                principalSchema: "Index",
                principalTable: "SeamFace",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId1",
                principalSchema: "Index",
                principalTable: "SeamFace",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "CuttingThicknessId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "LongwallParametersId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "MaterialType",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "SeamFaceId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AlterColumn<Guid>(
                name: "SupportStepId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "PassportId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "InsertItemId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
