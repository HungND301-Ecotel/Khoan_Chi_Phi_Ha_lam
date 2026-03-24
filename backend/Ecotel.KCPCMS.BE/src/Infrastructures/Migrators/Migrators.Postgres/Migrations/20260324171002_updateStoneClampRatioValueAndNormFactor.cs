using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateStoneClampRatioValueAndNormFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMaterialCost_StoneClampRatio_StoneClampRatioId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropForeignKey(
                name: "FK_StoneClampRatio_Hardness_HardnessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropForeignKey(
                name: "FK_StoneClampRatio_ProductionProcess_ProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropIndex(
                name: "IX_StoneClampRatio_ProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropColumn(
                name: "CoefficientValue",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropColumn(
                name: "ProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.RenameColumn(
                name: "StoneClampRatioId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                newName: "NormFactorId");

            migrationBuilder.Sql(@"
                UPDATE ""Pricing"".""PlannedMaterialCost"" 
                SET ""NormFactorId"" = NULL 
                WHERE ""NormFactorId"" NOT IN (SELECT ""Id"" FROM ""Index"".""NormFactor"");
            ");

            migrationBuilder.RenameIndex(
                name: "IX_PlannedMaterialCost_StoneClampRatioId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                newName: "IX_PlannedMaterialCost_NormFactorId");

            migrationBuilder.AlterColumn<Guid>(
                name: "HardnessId",
                schema: "Index",
                table: "StoneClampRatio",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoneClampRatio_ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_StoneClampRatio_Value",
                schema: "Index",
                table: "StoneClampRatio",
                column: "Value",
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "TargetHardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMaterialCost_NormFactor_NormFactorId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "NormFactorId",
                principalSchema: "Index",
                principalTable: "NormFactor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StoneClampRatio_Hardness_HardnessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StoneClampRatio_ProductionProcess_ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "ProductionProcessId",
                principalSchema: "Index",
                principalTable: "ProductionProcess",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropForeignKey(
                name: "FK_PlannedMaterialCost_NormFactor_NormFactorId",
                schema: "Pricing",
                table: "PlannedMaterialCost");

            migrationBuilder.DropForeignKey(
                name: "FK_StoneClampRatio_Hardness_HardnessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropForeignKey(
                name: "FK_StoneClampRatio_ProductionProcess_ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropIndex(
                name: "IX_StoneClampRatio_ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropIndex(
                name: "IX_StoneClampRatio_Value",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropColumn(
                name: "ProductionProcessId",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.RenameColumn(
                name: "NormFactorId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                newName: "StoneClampRatioId");

            migrationBuilder.RenameIndex(
                name: "IX_PlannedMaterialCost_NormFactorId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                newName: "IX_PlannedMaterialCost_StoneClampRatioId");

            migrationBuilder.AlterColumn<Guid>(
                name: "HardnessId",
                schema: "Index",
                table: "StoneClampRatio",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CoefficientValue",
                schema: "Index",
                table: "StoneClampRatio",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "ProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StoneClampRatio_ProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "TargetHardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlannedMaterialCost_StoneClampRatio_StoneClampRatioId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "StoneClampRatioId",
                principalSchema: "Index",
                principalTable: "StoneClampRatio",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StoneClampRatio_Hardness_HardnessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "HardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_StoneClampRatio_ProductionProcess_ProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "ProcessId",
                principalSchema: "Index",
                principalTable: "ProductionProcess",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
