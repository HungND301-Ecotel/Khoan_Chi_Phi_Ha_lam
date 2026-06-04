using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class dropLegacyTunnelMaterialUnitPriceRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DELETE FROM "Pricing"."MaterialUnitPrice" mup
            WHERE mup."MaterialType" IN (1, 2, 3)
              AND EXISTS (
                  SELECT 1
                  FROM "Pricing"."MaterialUnitPriceAssignmentCode" muac
                  LEFT JOIN "Index"."Material" m
                    ON m."Id" = muac."MaterialId"
                   AND m."DeletedOn" IS NULL
                  LEFT JOIN "Index"."AssignmentCode" ac
                    ON ac."Id" = muac."AssignmentCodeId"
                   AND ac."DeletedOn" IS NULL
                  WHERE muac."MaterialUnitPriceId" = mup."Id"
                    AND muac."DeletedOn" IS NULL
                    AND (
                        muac."MaterialId" IS NULL
                        OR m."Id" IS NULL
                        OR ac."Id" IS NULL
                        OR NOT (
                            m."AssigmentCodeId" = muac."AssignmentCodeId"
                            OR EXISTS (
                                SELECT 1
                                FROM "Index"."AssignmentCodeMaterial" acm
                                WHERE acm."MaterialId" = muac."MaterialId"
                                  AND acm."AssignmentCodeId" = muac."AssignmentCodeId"
                                  AND acm."DeletedOn" IS NULL
                            )
                        )
                    )
              );
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
