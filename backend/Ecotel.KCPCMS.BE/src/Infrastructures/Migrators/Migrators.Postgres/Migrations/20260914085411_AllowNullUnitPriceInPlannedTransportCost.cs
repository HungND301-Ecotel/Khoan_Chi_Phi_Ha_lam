using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullUnitPriceInPlannedTransportCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedTransportCost_ExactlyOneUnitPriceReference",
                schema: "Pricing",
                table: "PlannedTransportCost");

            migrationBuilder.CreateCheckConstraint(
                name: "CK_PlannedTransportCost_AtMostOneUnitPriceReference",
                schema: "Pricing",
                table: "PlannedTransportCost",
                sql: "(\"TransportUnitPriceId\" IS NULL OR \"MechanizedTransportUnitPriceDetailId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlannedTransportCost_AtMostOneUnitPriceReference",
                schema: "Pricing",
                table: "PlannedTransportCost");

            migrationBuilder.CreateCheckConstraint(
                name: "CK_PlannedTransportCost_ExactlyOneUnitPriceReference",
                schema: "Pricing",
                table: "PlannedTransportCost",
                sql: "((\"TransportUnitPriceId\" IS NOT NULL AND \"MechanizedTransportUnitPriceDetailId\" IS NULL) OR (\"TransportUnitPriceId\" IS NULL AND \"MechanizedTransportUnitPriceDetailId\" IS NOT NULL))");
        }
    }
}
