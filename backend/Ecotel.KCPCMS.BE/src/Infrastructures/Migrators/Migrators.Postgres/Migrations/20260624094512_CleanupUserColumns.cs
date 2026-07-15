using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    public partial class CleanupUserColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Avatar", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Cccd", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "District", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Dob", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Fullname", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Gender", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Province", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "StreetAddress", schema: "Identity", table: "Users");
            migrationBuilder.DropColumn(name: "Ward", schema: "Identity", table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Avatar", schema: "Identity", table: "Users", type: "character varying(256)", maxLength: 256, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Cccd", schema: "Identity", table: "Users", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<string>(name: "District", schema: "Identity", table: "Users", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<DateOnly>(name: "Dob", schema: "Identity", table: "Users", type: "date", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Fullname", schema: "Identity", table: "Users", type: "character varying(120)", maxLength: 120, nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "Gender", schema: "Identity", table: "Users", type: "boolean", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Province", schema: "Identity", table: "Users", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<string>(name: "StreetAddress", schema: "Identity", table: "Users", type: "character varying(255)", maxLength: 255, nullable: true);
            migrationBuilder.AddColumn<string>(name: "Ward", schema: "Identity", table: "Users", type: "character varying(255)", maxLength: 255, nullable: true);
        }
    }
}