using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateCustomeCostQuaterMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Quarter",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                newName: "Month");

            migrationBuilder.RenameIndex(
                name: "IX_LumpSumQuarterCustomCost_Year_Quarter_ProcessGroupId",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                newName: "IX_LumpSumQuarterCustomCost_Year_Month_ProcessGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Month",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                newName: "Quarter");

            migrationBuilder.RenameIndex(
                name: "IX_LumpSumQuarterCustomCost_Year_Month_ProcessGroupId",
                schema: "Production",
                table: "LumpSumQuarterCustomCost",
                newName: "IX_LumpSumQuarterCustomCost_Year_Quarter_ProcessGroupId");
        }
    }
}
