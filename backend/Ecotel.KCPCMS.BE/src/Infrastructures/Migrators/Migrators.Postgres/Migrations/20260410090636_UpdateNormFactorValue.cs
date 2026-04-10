using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNormFactorValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_NormFactor_TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropColumn(
                name: "TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropColumn(
                name: "Value",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Value",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                columns: new[] { "NormFactorId", "AssignmentCodeId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "TargetHardnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactorAssignmentCode_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "TargetHardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactorAssignmentCode_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropColumn(
                name: "TargetHardnessId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropColumn(
                name: "Value",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.AddColumn<Guid>(
                name: "TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Value",
                schema: "Index",
                table: "NormFactor",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "NormFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactor_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "TargetHardnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "TargetHardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
