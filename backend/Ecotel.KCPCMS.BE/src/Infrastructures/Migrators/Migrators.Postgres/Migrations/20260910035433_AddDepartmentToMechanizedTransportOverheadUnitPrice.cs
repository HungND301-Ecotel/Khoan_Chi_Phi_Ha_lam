using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddDepartmentToMechanizedTransportOverheadUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("1287742d-3b3a-4555-9462-ce342595b688"));

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportOverheadUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_MechanizedTransportOverheadUnitPrice_Department_DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice",
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
                name: "FK_MechanizedTransportOverheadUnitPrice_Department_DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MechanizedTransportOverheadUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice");
        }
    }
}
