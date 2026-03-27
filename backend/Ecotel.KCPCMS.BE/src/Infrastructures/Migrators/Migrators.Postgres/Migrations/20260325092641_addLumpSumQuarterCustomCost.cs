using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addLumpSumQuarterCustomCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LumpSumQuarterCustomCost",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Quarter = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomName = table.Column<string>(type: "text", nullable: false),
                    ActualQuantity = table.Column<double>(type: "double precision", nullable: false),
                    MaterialUnitPrice = table.Column<double>(type: "double precision", nullable: false),
                    MaintainUnitPrice = table.Column<double>(type: "double precision", nullable: false),
                    ElectricityUnitPrice = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LumpSumQuarterCustomCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LumpSumQuarterCustomCost_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LumpSumQuarterCustomCost_ProcessGroupId",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LumpSumQuarterCustomCost_Year_Quarter_ProcessGroupId",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                columns: new[] { "Year", "Quarter", "ProcessGroupId" },
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LumpSumQuarterCustomCost",
                schema: "Production");
        }
    }
}
