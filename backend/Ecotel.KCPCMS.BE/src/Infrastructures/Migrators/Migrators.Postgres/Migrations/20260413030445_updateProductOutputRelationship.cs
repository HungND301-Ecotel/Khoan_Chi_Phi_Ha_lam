using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateProductOutputRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutput_Department_DepartmentId",
                schema: "Production",
                table: "ProductionOutput");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutput_Department_DepartmentId",
                schema: "Production",
                table: "ProductionOutput",
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
                name: "FK_ProductionOutput_Department_DepartmentId",
                schema: "Production",
                table: "ProductionOutput");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutput_Department_DepartmentId",
                schema: "Production",
                table: "ProductionOutput",
                column: "DepartmentId",
                principalSchema: "Index",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
