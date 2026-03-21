using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductionOrderStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductionOrder_Value",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.RenameColumn(
                name: "Value",
                schema: "Index",
                table: "ProductionOrder",
                newName: "Name");

            migrationBuilder.AddColumn<Guid>(
                name: "CodeId",
                schema: "Index",
                table: "ProductionOrder",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Index",
                table: "ProductionOrder",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Index",
                table: "ProductionOrder",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_CodeId",
                schema: "Index",
                table: "ProductionOrder",
                column: "CodeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrder_Code_CodeId",
                schema: "Index",
                table: "ProductionOrder",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrder_Code_CodeId",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrder_CodeId",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.DropColumn(
                name: "CodeId",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "Index",
                table: "ProductionOrder",
                newName: "Value");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_Value",
                schema: "Index",
                table: "ProductionOrder",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
