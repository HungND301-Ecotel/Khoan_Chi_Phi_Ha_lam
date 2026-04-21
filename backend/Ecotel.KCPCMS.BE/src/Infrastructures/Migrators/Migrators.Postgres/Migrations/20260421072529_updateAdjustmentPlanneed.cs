using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAdjustmentPlanneed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "AdjustmentFactorDescriptionId",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AdjustmentFactorId",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CustomValue",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AdjustmentFactorDescriptionId",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "AdjustmentFactorId",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CustomValue",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentF~1",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                column: "AdjustmentFactorId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlannedMaintainCostAdjustmentFactorDescription_CustomOrRefe~",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                sql: "\r\n                    (\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NOT NULL AND\r\n                            \"AdjustmentFactorId\" IS NULL AND\r\n                            \"CustomValue\" IS NULL\r\n                        )\r\n                        OR\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NULL AND\r\n                            \"AdjustmentFactorId\" IS NOT NULL AND\r\n                            \"CustomValue\" IS NOT NULL\r\n                        )\r\n                    )\r\n                ");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactorDescription_Adjustme~1",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                column: "AdjustmentFactorId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_PlannedElectricityCostAdjustmentFactorDescription_CustomOrR~",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                sql: "\r\n                    (\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NOT NULL AND\r\n                            \"AdjustmentFactorId\" IS NULL AND\r\n                            \"CustomValue\" IS NULL\r\n                        )\r\n                        OR\r\n                        (\r\n                            \"AdjustmentFactorDescriptionId\" IS NULL AND\r\n                            \"AdjustmentFactorId\" IS NOT NULL AND\r\n                            \"CustomValue\" IS NOT NULL\r\n                        )\r\n                    )\r\n                ");

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedElectricityCostAdjustmentFactorDescription_Adjustme~1",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                column: "AdjustmentFactorId",
                principalSchema: "Index",
                principalTable: "AdjustmentFactor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentF~1",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                column: "AdjustmentFactorId",
                principalSchema: "Index",
                principalTable: "AdjustmentFactor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlannedElectricityCostAdjustmentFactorDescription_Adjustme~1",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentF~1",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription");

            migrationBuilder.DropIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentF~1",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedMaintainCostAdjustmentFactorDescription_CustomOrRefe~",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription");

            migrationBuilder.DropIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactorDescription_Adjustme~1",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedElectricityCostAdjustmentFactorDescription_CustomOrR~",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription");

            migrationBuilder.DropColumn(
                name: "AdjustmentFactorId",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription");

            migrationBuilder.DropColumn(
                name: "CustomValue",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription");

            migrationBuilder.DropColumn(
                name: "AdjustmentFactorId",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription");

            migrationBuilder.DropColumn(
                name: "CustomValue",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription");

            migrationBuilder.AlterColumn<Guid>(
                name: "AdjustmentFactorDescriptionId",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AdjustmentFactorDescriptionId",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
