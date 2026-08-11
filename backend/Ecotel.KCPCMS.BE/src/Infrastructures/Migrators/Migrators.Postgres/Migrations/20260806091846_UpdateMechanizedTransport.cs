using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMechanizedTransport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MechanizedTransportUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MechanizedTransportUnitPrice_CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "ElectricityNorm",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<decimal>(
                name: "ElectricityNorm",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MechanizedTransportUnitPrice_Code_CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "CodeId",
                principalSchema: "Index",
                principalTable: "Code",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
