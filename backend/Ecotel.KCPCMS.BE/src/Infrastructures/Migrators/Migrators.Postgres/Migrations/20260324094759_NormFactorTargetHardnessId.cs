using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class NormFactorTargetHardnessId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactor_NormFactor_ReferenceNormFactorId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.RenameColumn(
                name: "ReferenceNormFactorId",
                schema: "Index",
                table: "NormFactor",
                newName: "TargetHardnessId");

            migrationBuilder.RenameIndex(
                name: "IX_NormFactor_ReferenceNormFactorId",
                schema: "Index",
                table: "NormFactor",
                newName: "IX_NormFactor_TargetHardnessId");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "TargetHardnessId",
                principalSchema: "Index",
                principalTable: "Hardness",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NormFactor_Hardness_TargetHardnessId",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.RenameColumn(
                name: "TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                newName: "ReferenceNormFactorId");

            migrationBuilder.RenameIndex(
                name: "IX_NormFactor_TargetHardnessId",
                schema: "Index",
                table: "NormFactor",
                newName: "IX_NormFactor_ReferenceNormFactorId");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactor_NormFactor_ReferenceNormFactorId",
                schema: "Index",
                table: "NormFactor",
                column: "ReferenceNormFactorId",
                principalSchema: "Index",
                principalTable: "NormFactor",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
