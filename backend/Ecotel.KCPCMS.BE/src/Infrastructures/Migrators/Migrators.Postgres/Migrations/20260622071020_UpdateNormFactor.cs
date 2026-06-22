using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    public partial class UpdateNormFactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

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

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                columns: new[] { "NormFactorId", "AssignmentCodeId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId_MaterialId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.Sql(@"
        DELETE FROM ""Index"".""NormFactorAssignmentCode""
        WHERE ""Id"" NOT IN (
            SELECT DISTINCT ON (""NormFactorId"", ""AssignmentCodeId"") ""Id""
            FROM ""Index"".""NormFactorAssignmentCode""
            WHERE ""DeletedOn"" IS NULL
            ORDER BY ""NormFactorId"", ""AssignmentCodeId"", ""CreatedOn"" DESC
        )
        AND ""DeletedOn"" IS NULL;
    ");

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

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                columns: new[] { "NormFactorId", "AssignmentCodeId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}