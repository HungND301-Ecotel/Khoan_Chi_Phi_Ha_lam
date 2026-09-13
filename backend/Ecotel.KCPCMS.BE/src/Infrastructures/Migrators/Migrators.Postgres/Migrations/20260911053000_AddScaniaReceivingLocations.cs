using System;
using EfCore.Persistence.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260911053000_AddScaniaReceivingLocations")]
    public partial class AddScaniaReceivingLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScaniaTruckUnitPriceReceivingLocations",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScaniaTruckUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScaniaTruckUnitPriceReceivingLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScaniaTruckUnitPriceReceivingLocations_MechanizedTransportUnitPrice_ScaniaTruckUnitPriceId",
                        column: x => x.ScaniaTruckUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MechanizedTransportUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScaniaTruckUnitPriceReceivingLocations_TransportLocation_TransportLocationId",
                        column: x => x.TransportLocationId,
                        principalSchema: "Index",
                        principalTable: "TransportLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScaniaTruckUnitPriceReceivingLocations_ScaniaTruckUnitPriceId",
                schema: "Pricing",
                table: "ScaniaTruckUnitPriceReceivingLocations",
                column: "ScaniaTruckUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ScaniaTruckUnitPriceReceivingLocations_TransportLocationId",
                schema: "Pricing",
                table: "ScaniaTruckUnitPriceReceivingLocations",
                column: "TransportLocationId");

            migrationBuilder.Sql(@"INSERT INTO ""Pricing"".""ScaniaTruckUnitPriceReceivingLocations"" (""Id"", ""ScaniaTruckUnitPriceId"", ""TransportLocationId"", ""CreatedBy"", ""CreatedOn"", ""LastModifiedBy"") SELECT gen_random_uuid(), ""Id"", ""ReceivingLocationId"", ""CreatedBy"", ""CreatedOn"", ""LastModifiedBy"" FROM ""Pricing"".""MechanizedTransportUnitPrice"" WHERE ""ReceivingLocationId"" IS NOT NULL;");

            migrationBuilder.DropForeignKey(
                name: "FK_MechanizedTransportUnitPrice_TransportLocation_ReceivingLoc~",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_MechanizedTransportUnitPrice_ReceivingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "ReceivingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReceivingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_ReceivingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "ReceivingLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_MechanizedTransportUnitPrice_TransportLocation_ReceivingLoc~",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "ReceivingLocationId",
                principalSchema: "Index",
                principalTable: "TransportLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropTable(
                name: "ScaniaTruckUnitPriceReceivingLocations",
                schema: "Pricing");
        }
    }
}
