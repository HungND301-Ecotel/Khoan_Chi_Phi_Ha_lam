using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultiplePeriodsPerCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_SlideUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_SlideUnitPrice_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "CodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SlideUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_SlideUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_SlideUnitPrice_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SlideUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
