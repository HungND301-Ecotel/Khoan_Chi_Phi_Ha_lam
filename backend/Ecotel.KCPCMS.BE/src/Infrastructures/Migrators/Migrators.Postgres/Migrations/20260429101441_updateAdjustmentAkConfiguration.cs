using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateAdjustmentAkConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAkDiff",
                schema: "Index",
                table: "AkFactorConfig");

            migrationBuilder.DropColumn(
                name: "MinAkDiff",
                schema: "Index",
                table: "AkFactorConfig");

            migrationBuilder.RenameColumn(
                name: "MinAdjustmentRate",
                schema: "Index",
                table: "AkFactorConfig",
                newName: "AkDiffValue");

            migrationBuilder.RenameColumn(
                name: "MaxAdjustmentRate",
                schema: "Index",
                table: "AkFactorConfig",
                newName: "AdjustmentRate");

            migrationBuilder.AddColumn<string>(
                name: "AkDiffOperator",
                schema: "Index",
                table: "AkFactorConfig",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AkDiffOperator",
                schema: "Index",
                table: "AkFactorConfig");

            migrationBuilder.RenameColumn(
                name: "AkDiffValue",
                schema: "Index",
                table: "AkFactorConfig",
                newName: "MinAdjustmentRate");

            migrationBuilder.RenameColumn(
                name: "AdjustmentRate",
                schema: "Index",
                table: "AkFactorConfig",
                newName: "MaxAdjustmentRate");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAkDiff",
                schema: "Index",
                table: "AkFactorConfig",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinAkDiff",
                schema: "Index",
                table: "AkFactorConfig",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }
    }
}
