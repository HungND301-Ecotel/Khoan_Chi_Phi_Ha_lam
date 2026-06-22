using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNormFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "MaterialId");

            migrationBuilder.Sql("DELETE FROM \"Index\".\"NormFactorAssignmentCode\";");

            migrationBuilder.AddForeignKey(
                name: "FK_NormFactorAssignmentCode_Material_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
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
                name: "FK_NormFactorAssignmentCode_Material_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode");
        }
    }
}
