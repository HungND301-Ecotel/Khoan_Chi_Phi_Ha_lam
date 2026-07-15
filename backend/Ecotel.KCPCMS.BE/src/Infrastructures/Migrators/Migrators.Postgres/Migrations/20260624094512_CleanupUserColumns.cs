using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    public partial class CleanupUserColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Avatar\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Cccd\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"District\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Dob\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Fullname\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Gender\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Province\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"StreetAddress\";");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" DROP COLUMN IF EXISTS \"Ward\";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Avatar\" character varying(256) NOT NULL DEFAULT '';");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Cccd\" character varying(255);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"District\" character varying(255);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Dob\" date;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Fullname\" character varying(120) NOT NULL DEFAULT '';");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Gender\" boolean;");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Province\" character varying(255);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"StreetAddress\" character varying(255);");
            migrationBuilder.Sql(
                "ALTER TABLE \"Identity\".\"Users\" ADD COLUMN IF NOT EXISTS \"Ward\" character varying(255);");
        }
    }
}