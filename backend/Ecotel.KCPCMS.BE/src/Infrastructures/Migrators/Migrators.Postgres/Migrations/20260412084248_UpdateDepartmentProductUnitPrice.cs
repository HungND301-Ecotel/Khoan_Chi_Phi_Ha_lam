using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDepartmentProductUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnitPrice_Department_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "DepartmentId",
                principalSchema: "Index",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnitPrice_Department_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Pricing",
                table: "ProductUnitPrice");
        }
    }
}
