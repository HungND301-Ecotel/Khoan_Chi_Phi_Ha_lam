using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOutputTransportLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionOutputTransportLine",
                schema: "Production",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionOutputProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransportRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOutputTransportLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOutputTransportLine_AssignmentCode_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOutputTransportLine_ProductionOutputProcessGroup_~",
                        column: x => x.ProductionOutputProcessGroupId,
                        principalSchema: "Production",
                        principalTable: "ProductionOutputProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOutputTransportLine_ProductionProcess_ProductionP~",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionOutputTransportLine_TransportRoute_TransportRoute~",
                        column: x => x.TransportRouteId,
                        principalSchema: "Index",
                        principalTable: "TransportRoute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_EquipmentId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_ProductionOutputProcessGroupId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "ProductionOutputProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_ProductionProcessId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_TransportRouteId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "TransportRouteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionOutputTransportLine",
                schema: "Production");
        }
    }
}
