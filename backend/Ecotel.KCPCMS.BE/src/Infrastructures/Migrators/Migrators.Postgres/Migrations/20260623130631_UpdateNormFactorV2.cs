using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    public partial class UpdateNormFactorV2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoneClampRatioId",
                schema: "Index",
                table: "NormFactor",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

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

            migrationBuilder.AlterColumn<Guid>(
                name: "StoneClampRatioId",
                schema: "Index",
                table: "NormFactor",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

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