using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateMaterialUnitPriceOtherMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddOtherMaterialUnitPrice",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.AlterColumn<double>(
                name: "OtherMaterialValue",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "double precision",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "OtherMaterialValue",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AddOtherMaterialUnitPrice",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
