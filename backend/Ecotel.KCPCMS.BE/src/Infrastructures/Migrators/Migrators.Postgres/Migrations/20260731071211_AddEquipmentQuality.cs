using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipmentQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportUnitPrice_AdjustmentFactorDescription_AdjustmentFa~",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_TransportUnitPrice_AdjustmentFactorDescriptionId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "AdjustmentFactorDescriptionId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentQuality",
                schema: "Pricing",
                table: "TransportUnitPrice",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EquipmentQuality",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.AddColumn<Guid>(
                name: "AdjustmentFactorDescriptionId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_AdjustmentFactorDescriptionId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportUnitPrice_AdjustmentFactorDescription_AdjustmentFa~",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "AdjustmentFactorDescriptionId",
                principalSchema: "Index",
                principalTable: "AdjustmentFactorDescription",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
