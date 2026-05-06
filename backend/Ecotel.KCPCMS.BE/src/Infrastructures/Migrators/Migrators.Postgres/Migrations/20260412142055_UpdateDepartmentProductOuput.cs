using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDepartmentProductOuput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Production",
                table: "ProductionOutput",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutput_DepartmentId",
                schema: "Production",
                table: "ProductionOutput",
                column: "DepartmentId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutput_Department_DepartmentId",
                schema: "Production",
                table: "ProductionOutput");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutput_DepartmentId",
                schema: "Production",
                table: "ProductionOutput");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Production",
                table: "ProductionOutput");
        }
    }
}
