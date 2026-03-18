using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class mergeCuttingThicknessValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "From",
                schema: "Index",
                table: "CuttingThickness");

            migrationBuilder.RenameColumn(
                name: "To",
                schema: "Index",
                table: "CuttingThickness",
                newName: "Value");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "Index",
                table: "CuttingThickness",
                newName: "To");

            migrationBuilder.AddColumn<string>(
                name: "From",
                schema: "Index",
                table: "CuttingThickness",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
