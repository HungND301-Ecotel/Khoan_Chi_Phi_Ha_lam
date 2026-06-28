using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addLongTermAnchorSeedCategoryReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "AssignmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_LongTermAnchorSeedItem_ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "ProductionOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermAnchorSeedItem_AssignmentCode_AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "AssignmentCodeId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermAnchorSeedItem_ProductionOrder_ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "ProductionOrderId",
                principalSchema: "Index",
                principalTable: "ProductionOrder",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LongTermAnchorSeedItem_AssignmentCode_AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropForeignKey(
                name: "FK_LongTermAnchorSeedItem_ProductionOrder_ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropIndex(
                name: "IX_LongTermAnchorSeedItem_AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropIndex(
                name: "IX_LongTermAnchorSeedItem_ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropColumn(
                name: "AssignmentCodeId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");
        }
    }
}
