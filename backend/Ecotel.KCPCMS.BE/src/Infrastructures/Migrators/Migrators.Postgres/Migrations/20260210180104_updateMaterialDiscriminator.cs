using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateMaterialDiscriminator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType""
                TYPE integer
                USING CASE
                    WHEN ""MaterialType"" = 'TunnelExcavation' THEN 1
                    WHEN ""MaterialType"" = 'Longwall' THEN 2
                    ELSE 0
                END
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType"" SET DEFAULT 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType"" DROP DEFAULT;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType""
                TYPE varchar(21)
                USING CASE
                    WHEN ""MaterialType"" = 1 THEN 'TunnelExcavation'
                    WHEN ""MaterialType"" = 2 THEN 'Longwall'
                    ELSE 'Unknown'
                END;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Pricing"".""MaterialUnitPrice""
                ALTER COLUMN ""MaterialType"" SET DEFAULT 'TunnelExcavation';
            ");
        }
    }
}
