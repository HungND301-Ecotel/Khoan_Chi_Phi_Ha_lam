using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addLowValuePerishableSupplyUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LowValuePerishableSupplyInclusion",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LowValuePerishableSupplyUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TotalPrice = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LowValuePerishableSupplyUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LowValuePerishableSupplyUnitPrice_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "Index",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LowValuePerishableSupplyUnitPrice_ProcessGroup_ProcessGroup~",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LowValuePerishableSupplyUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "LowValuePerishableSupplyUnitPrice",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LowValuePerishableSupplyUnitPrice_ProcessGroupId_Department~",
                schema: "Pricing",
                table: "LowValuePerishableSupplyUnitPrice",
                columns: new[] { "ProcessGroupId", "DepartmentId", "StartMonth", "Type", "EndMonth" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LowValuePerishableSupplyUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropColumn(
                name: "LowValuePerishableSupplyInclusion",
                schema: "Pricing",
                table: "PlannedMaterialCost");
        }
    }
}
