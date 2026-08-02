using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportRouteAndUnitPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TransportRoute",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSpecialLowVolume = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportRoute", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransportRoute_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransportRoute_ProductionProcess_ProductionProcessId",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransportUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransportRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomAdjustmentValue = table.Column<double>(type: "double precision", nullable: true),
                    MaterialFuelUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    PowerUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaintenanceUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsLowVolumeCase = table.Column<bool>(type: "boolean", nullable: false),
                    StartMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    EndMonth = table.Column<DateOnly>(type: "date", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransportUnitPrice", x => x.Id);
                    table.CheckConstraint("CK_TransportUnitPrice_AdjustmentConflict", "NOT (\"AdjustmentFactorDescriptionId\" IS NOT NULL AND \"CustomAdjustmentValue\" IS NOT NULL)");
                    table.CheckConstraint("CK_TransportUnitPrice_AtLeastOnePrice", "(\"MaterialFuelUnitPrice\" IS NOT NULL OR \"PowerUnitPrice\" IS NOT NULL OR \"MaintenanceUnitPrice\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_AdjustmentFactorDescription_AdjustmentFa~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalSchema: "Index",
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Index",
                        principalTable: "Product",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_ProductionProcess_ProductionProcessId",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_TransportRoute_TransportRouteId",
                        column: x => x.TransportRouteId,
                        principalSchema: "Index",
                        principalTable: "TransportRoute",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransportUnitPrice_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "Index",
                table: "FixedKey",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "DeletedBy", "DeletedOn", "Key", "LastModifiedBy", "LastModifiedOn", "Name", "Type" },
                values: new object[] { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, null, "VTL", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vận tải lò", 12 });

            migrationBuilder.CreateIndex(
                name: "IX_TransportRoute_CodeId",
                schema: "Index",
                table: "TransportRoute",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TransportRoute_ProductionProcessId",
                schema: "Index",
                table: "TransportRoute",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_AdjustmentFactorDescriptionId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_DepartmentId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_MaterialId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_ProductId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_ProductionProcessId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_TransportRouteId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "TransportRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_TransportUnitPrice_UnitOfMeasureId",
                schema: "Pricing",
                table: "TransportUnitPrice",
                column: "UnitOfMeasureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TransportUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "TransportRoute",
                schema: "Index");

            migrationBuilder.DeleteData(
                schema: "Index",
                table: "FixedKey",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        }
    }
}
