using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateSavingRateConfigInput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MinRevenue",
                schema: "Index",
                table: "SavingsRateConfig",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinSavingsRate",
                schema: "Index",
                table: "SavingsRateConfig",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevenueDisplay",
                schema: "Index",
                table: "SavingsRateConfig",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SavingsRateDisplay",
                schema: "Index",
                table: "SavingsRateConfig",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MinRevenue",
                schema: "Index",
                table: "SavingsRateConfig");

            migrationBuilder.DropColumn(
                name: "MinSavingsRate",
                schema: "Index",
                table: "SavingsRateConfig");

            migrationBuilder.DropColumn(
                name: "RevenueDisplay",
                schema: "Index",
                table: "SavingsRateConfig");

            migrationBuilder.DropColumn(
                name: "SavingsRateDisplay",
                schema: "Index",
                table: "SavingsRateConfig");
        }
    }
}
