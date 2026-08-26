using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddCargoTypeAndLocationsToTransportPlanLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DumpingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReceivingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "CargoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "DumpingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportPlanLine_ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "ReceivingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "CargoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_DumpingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "DumpingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "HaulDistanceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOutputTransportLine_ReceivingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "ReceivingLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputTransportLine_CargoType_CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "CargoTypeId",
                principalSchema: "Index",
                principalTable: "CargoType",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputTransportLine_HaulDistance_HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "HaulDistanceId",
                principalSchema: "Index",
                principalTable: "HaulDistance",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputTransportLine_TransportLocation_DumpingLoca~",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "DumpingLocationId",
                principalSchema: "Index",
                principalTable: "TransportLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOutputTransportLine_TransportLocation_ReceivingLo~",
                schema: "Production",
                table: "ProductionOutputTransportLine",
                column: "ReceivingLocationId",
                principalSchema: "Index",
                principalTable: "TransportLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportPlanLine_CargoType_CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "CargoTypeId",
                principalSchema: "Index",
                principalTable: "CargoType",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportPlanLine_TransportLocation_DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "DumpingLocationId",
                principalSchema: "Index",
                principalTable: "TransportLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportPlanLine_TransportLocation_ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine",
                column: "ReceivingLocationId",
                principalSchema: "Index",
                principalTable: "TransportLocation",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutputTransportLine_CargoType_CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutputTransportLine_HaulDistance_HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutputTransportLine_TransportLocation_DumpingLoca~",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOutputTransportLine_TransportLocation_ReceivingLo~",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportPlanLine_CargoType_CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportPlanLine_TransportLocation_DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportPlanLine_TransportLocation_ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropIndex(
                name: "IX_TransportPlanLine_CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropIndex(
                name: "IX_TransportPlanLine_DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropIndex(
                name: "IX_TransportPlanLine_ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutputTransportLine_CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutputTransportLine_DumpingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutputTransportLine_HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOutputTransportLine_ReceivingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropColumn(
                name: "CargoTypeId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropColumn(
                name: "DumpingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropColumn(
                name: "ReceivingLocationId",
                schema: "Pricing",
                table: "TransportPlanLine");

            migrationBuilder.DropColumn(
                name: "CargoTypeId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropColumn(
                name: "DumpingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropColumn(
                name: "HaulDistanceId",
                schema: "Production",
                table: "ProductionOutputTransportLine");

            migrationBuilder.DropColumn(
                name: "ReceivingLocationId",
                schema: "Production",
                table: "ProductionOutputTransportLine");
        }
    }
}
