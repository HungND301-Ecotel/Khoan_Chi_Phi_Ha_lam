using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateCuttingThickness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CuttingThickness_Value",
                schema: "Index",
                table: "CuttingThickness");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateIndex(
                name: "IX_CuttingThickness_Value",
                schema: "Index",
                table: "CuttingThickness",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
