using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddMechanizedTransportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransportRoute_ProductionProcess_ProductionProcessId",
                schema: "Index",
                table: "TransportRoute");

            migrationBuilder.DropForeignKey(
                name: "FK_TransportUnitPrice_UnitOfMeasure_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_TransportUnitPrice_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropIndex(
                name: "IX_TransportRoute_ProductionProcessId",
                schema: "Index",
                table: "TransportRoute");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice");

            migrationBuilder.DropColumn(
                name: "ProductionProcessId",
                schema: "Index",
                table: "TransportRoute");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CargoType",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CargoType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CargoType_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HaulDistance",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HaulDistance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MechanizedTransportOverheadUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    LowValuePerishableSupplyUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ElectricityNorm = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ElectricityUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MechanizedTransportOverheadUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportOverheadUnitPrice_ProcessGroup_ProcessGr~",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportLocation",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    LocationType = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportLocation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportLocation_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MechanizedTransportUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentQuality = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    CargoTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceivingLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    DumpingLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MechanizedTransportUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_AssignmentCode_AssignmentCodeId",
                        column: x => x.AssignmentCodeId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_CargoType_CargoTypeId",
                        column: x => x.CargoTypeId,
                        principalSchema: "Index",
                        principalTable: "CargoType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_ProductionProcess_ProductionPr~",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_TransportLocation_DumpingLocat~",
                        column: x => x.DumpingLocationId,
                        principalSchema: "Index",
                        principalTable: "TransportLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPrice_TransportLocation_ReceivingLoc~",
                        column: x => x.ReceivingLocationId,
                        principalSchema: "Index",
                        principalTable: "TransportLocation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MechanizedTransportUnitPriceDetail",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MechanizedTransportUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    HaulDistanceId = table.Column<Guid>(type: "uuid", nullable: true),
                    FuelUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PowerUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaintenanceUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MechanizedTransportUnitPriceDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPriceDetail_HaulDistance_HaulDistanc~",
                        column: x => x.HaulDistanceId,
                        principalSchema: "Index",
                        principalTable: "HaulDistance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MechanizedTransportUnitPriceDetail_MechanizedTransportUnitP~",
                        column: x => x.MechanizedTransportUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MechanizedTransportUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Index",
                table: "FixedKey",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "DeletedBy", "DeletedOn", "Key", "LastModifiedBy", "LastModifiedOn", "Name", "Type" },
                values: new object[] { new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "VTCG", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vận tải cơ giới", 13 });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProcess_UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_CargoType_CodeId",
                schema: "Index",
                table: "CargoType",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HaulDistance_Value",
                schema: "Index",
                table: "HaulDistance",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportOverheadUnitPrice_ProcessGroupId",
                schema: "Pricing",
                table: "MechanizedTransportOverheadUnitPrice",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_AssignmentCodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "AssignmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_CargoTypeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "CargoTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_CodeId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_DumpingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "DumpingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPrice_ReceivingLocationId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPrice",
                column: "ReceivingLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPriceDetail_HaulDistanceId",
                schema: "Pricing",
                table: "MechanizedTransportUnitPriceDetail",
                column: "HaulDistanceId");

            migrationBuilder.CreateIndex(
                name: "IX_MechanizedTransportUnitPriceDetail_MechanizedTransportUnitP~",
                schema: "Pricing",
                table: "MechanizedTransportUnitPriceDetail",
                column: "MechanizedTransportUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportLocation_CodeId",
                schema: "Index",
                table: "TransportLocation",
                column: "CodeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionProcess_UnitOfMeasure_UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess",
                column: "UnitOfMeasureId",
                principalSchema: "Index",
                principalTable: "UnitOfMeasure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionProcess_UnitOfMeasure_UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess");

            migrationBuilder.DropTable(
                name: "MechanizedTransportOverheadUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "MechanizedTransportUnitPriceDetail",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "HaulDistance",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "MechanizedTransportUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "CargoType",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "TransportLocation",
                schema: "Index");

            migrationBuilder.DropIndex(
                name: "IX_ProductionProcess_UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess");

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"));

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureId",
                schema: "Index",
                table: "ProductionProcess");

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductionProcessId",
                schema: "Index",
                table: "TransportRoute",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportRoute_ProductionProcessId",
                schema: "Index",
                table: "TransportRoute",
                column: "ProductionProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransportRoute_ProductionProcess_ProductionProcessId",
                schema: "Index",
                table: "TransportRoute",
                column: "ProductionProcessId",
                principalSchema: "Index",
                principalTable: "ProductionProcess",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransportUnitPrice_UnitOfMeasure_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "UnitOfMeasureId",
                principalSchema: "Index",
                principalTable: "UnitOfMeasure",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
