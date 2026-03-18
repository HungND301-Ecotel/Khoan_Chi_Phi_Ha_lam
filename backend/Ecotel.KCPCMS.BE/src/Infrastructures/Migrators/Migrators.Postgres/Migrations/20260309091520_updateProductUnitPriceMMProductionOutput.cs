using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class updateProductUnitPriceMMProductionOutput : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductUnitPrice_ProductionOutput_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_ProductUnitPrice_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice");

            migrationBuilder.CreateTable(
                name: "ProductUnitPriceProductionOutput",
                schema: "Pricing",
                columns: table => new
                {
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitPriceProductionOutput", x => new { x.ProductUnitPriceId, x.ProductionOutputId });
                    table.ForeignKey(
                        name: "FK_ProductUnitPriceProductionOutput_ProductUnitPrice_ProductUn~",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductUnitPriceProductionOutput_ProductionOutput_Productio~",
                        column: x => x.ProductionOutputId,
                        principalSchema: "Production",
                        principalTable: "ProductionOutput",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPriceProductionOutput_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPriceProductionOutput",
                column: "ProductionOutputId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductUnitPriceProductionOutput",
                schema: "Pricing");

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductionOutputId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductUnitPrice_ProductionOutput_ProductionOutputId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductionOutputId",
                principalSchema: "Production",
                principalTable: "ProductionOutput",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
