using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class removeSlideUnitPriceAssCodeQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                schema: "Pricing",
                table: "SlideUnitPriceAssignmentCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Quantity",
                schema: "Pricing",
                table: "SlideUnitPriceAssignmentCode",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
