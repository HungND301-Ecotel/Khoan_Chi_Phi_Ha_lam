using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTransportUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TransportUnitPrice_AdjustmentConflict",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "CustomAdjustmentValue",
                schema: "Pricing",
                table: "TransportUnitPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CustomAdjustmentValue",
                schema: "Pricing",
                table: "TransportUnitPrice",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TransportUnitPrice_AdjustmentConflict",
                schema: "Pricing",
                table: "TransportUnitPrice",
                sql: "NOT (\"AdjustmentFactorDescriptionId\" IS NOT NULL AND \"CustomAdjustmentValue\" IS NOT NULL)");
        }
    }
}
