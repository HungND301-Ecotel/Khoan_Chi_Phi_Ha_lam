using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleToAssignmentCodeMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Index"."Material"
                SET "MaterialType" = 1
                WHERE "MaterialType" = 2;
                """);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "Index",
                table: "AssignmentCodeMaterial",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "Index",
                table: "AssignmentCodeMaterial");
        }
    }
}
