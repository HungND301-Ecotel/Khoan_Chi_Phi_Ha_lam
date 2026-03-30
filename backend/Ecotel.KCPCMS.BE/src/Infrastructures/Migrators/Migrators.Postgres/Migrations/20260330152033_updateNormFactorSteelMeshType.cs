using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateNormFactorSteelMeshType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsageTime",
                schema: "Index",
                table: "Material");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoneClampRatioId",
                schema: "Index",
                table: "NormFactor",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "SteelMeshType",
                schema: "Index",
                table: "NormFactor",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SteelMeshType",
                schema: "Index",
                table: "NormFactor");

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

            migrationBuilder.AddColumn<decimal>(
                name: "UsageTime",
                schema: "Index",
                table: "Material",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
