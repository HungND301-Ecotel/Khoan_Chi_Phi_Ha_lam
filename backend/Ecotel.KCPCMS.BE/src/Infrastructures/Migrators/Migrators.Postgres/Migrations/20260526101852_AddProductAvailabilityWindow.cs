using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductAvailabilityWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Index",
                table: "Product",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2099, 12, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Index",
                table: "Product",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(2000, 1, 1));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Index",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Index",
                table: "Product");
        }
    }
}
