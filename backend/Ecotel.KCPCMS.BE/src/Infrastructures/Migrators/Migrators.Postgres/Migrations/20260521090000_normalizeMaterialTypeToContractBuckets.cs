using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    public partial class normalizeMaterialTypeToContractBuckets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Index"."Material"
                SET "MaterialType" = 1
                WHERE "MaterialType" NOT IN (1, 2);
                """);

            migrationBuilder.Sql("""
                UPDATE "Index"."Cost"
                SET "CostType" = 3
                WHERE "CostType" != 3 AND "MaterialId" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
