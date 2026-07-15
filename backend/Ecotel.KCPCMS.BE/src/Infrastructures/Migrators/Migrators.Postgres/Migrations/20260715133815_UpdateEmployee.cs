using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "Index",
                table: "Position");

            migrationBuilder.DropColumn(
                name: "District",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Province",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "StreetAddress",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Ward",
                schema: "Index",
                table: "Employee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "Index",
                table: "Position",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "District",
                schema: "Index",
                table: "Employee",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                schema: "Index",
                table: "Employee",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StreetAddress",
                schema: "Index",
                table: "Employee",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                schema: "Index",
                table: "Employee",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
