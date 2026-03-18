using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateEletricityUnitPriceProp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AlterColumn<double>(
                name: "MonthlyElectricityCost",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageMonthlyTunnelProduction",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.AddColumn<int>(
                name: "ElectricityType",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Kdt",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Kyc",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Pdm",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TunnelElectricityUnitPriceEquipment_AverageMonthlyTunnelProduc~",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkingDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WorkingHour",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElectricityType",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "Kdt",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "Kyc",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "Pdm",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "TunnelElectricityUnitPriceEquipment_AverageMonthlyTunnelProduc~",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "WorkingDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "WorkingHour",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.AddColumn<Guid>(
                name: "CuttingThicknessId1",
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

            migrationBuilder.AddColumn<Guid>(
                name: "SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "MonthlyElectricityCost",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AverageMonthlyTunnelProduction",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId1");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId1");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId1");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_CuttingThickness_CuttingThicknessId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CuttingThicknessId1",
                principalSchema: "Index",
                principalTable: "CuttingThickness",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_LongwallParameters_LongwallParametersId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "LongwallParametersId1",
                principalSchema: "Index",
                principalTable: "LongwallParameters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_SeamFace_SeamFaceId1",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SeamFaceId1",
                principalSchema: "Index",
                principalTable: "SeamFace",
                principalColumn: "Id");
        }
    }
}
