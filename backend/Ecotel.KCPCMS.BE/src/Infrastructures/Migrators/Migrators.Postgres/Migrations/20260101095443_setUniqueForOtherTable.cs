using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class setUniqueForOtherTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UnitOfMeasure_Name",
                schema: "Index",
                table: "UnitOfMeasure",
                column: "Name",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SupportStep_Value",
                schema: "Index",
                table: "SupportStep",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Passport_Name_Sd_Sc",
                schema: "Index",
                table: "Passport",
                columns: new[] { "Name", "Sd", "Sc" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InsertItem_Value",
                schema: "Index",
                table: "InsertItem",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Hardness_Value",
                schema: "Index",
                table: "Hardness",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitOfMeasure_Name",
                schema: "Index",
                table: "UnitOfMeasure");

            migrationBuilder.DropIndex(
                name: "IX_SupportStep_Value",
                schema: "Index",
                table: "SupportStep");

            migrationBuilder.DropIndex(
                name: "IX_Passport_Name_Sd_Sc",
                schema: "Index",
                table: "Passport");

            migrationBuilder.DropIndex(
                name: "IX_InsertItem_Value",
                schema: "Index",
                table: "InsertItem");

            migrationBuilder.DropIndex(
                name: "IX_Hardness_Value",
                schema: "Index",
                table: "Hardness");
        }
    }
}
