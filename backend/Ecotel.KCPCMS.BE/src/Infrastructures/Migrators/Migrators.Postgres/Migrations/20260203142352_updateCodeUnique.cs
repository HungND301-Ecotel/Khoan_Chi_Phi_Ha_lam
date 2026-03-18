using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateCodeUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Code_Value",
                schema: "Index",
                table: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Code_Value",
                schema: "Index",
                table: "Code",
                column: "Value",
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Code_Value",
                schema: "Index",
                table: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Code_Value",
                schema: "Index",
                table: "Code",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
