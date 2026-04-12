using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductUnitPriceIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                columns: new[] { "ProductId", "ScenarioType", "DepartmentId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductId_ScenarioType",
                schema: "Pricing",
                table: "ProductUnitPrice",
                columns: new[] { "ProductId", "ScenarioType" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
