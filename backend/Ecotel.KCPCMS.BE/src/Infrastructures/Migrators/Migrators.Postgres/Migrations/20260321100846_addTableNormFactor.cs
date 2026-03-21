using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addTableNormFactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NormFactor",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardnessId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoneClampRatioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    ReferenceNormFactorId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NormFactor_Hardness_HardnessId",
                        column: x => x.HardnessId,
                        principalSchema: "Index",
                        principalTable: "Hardness",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NormFactor_NormFactor_ReferenceNormFactorId",
                        column: x => x.ReferenceNormFactorId,
                        principalSchema: "Index",
                        principalTable: "NormFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NormFactor_ProductionProcess_ProductionProcessId",
                        column: x => x.ProductionProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NormFactor_StoneClampRatio_StoneClampRatioId",
                        column: x => x.StoneClampRatioId,
                        principalSchema: "Index",
                        principalTable: "StoneClampRatio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NormFactorAssignmentCode",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NormFactorAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NormFactorAssignmentCode_AssignmentCode_AssignmentCodeId",
                        column: x => x.AssignmentCodeId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NormFactorAssignmentCode_NormFactor_NormFactorId",
                        column: x => x.NormFactorId,
                        principalSchema: "Index",
                        principalTable: "NormFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NormFactor_HardnessId",
                schema: "Index",
                table: "NormFactor",
                column: "HardnessId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactor_ProductionProcessId",
                schema: "Index",
                table: "NormFactor",
                column: "ProductionProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactor_ReferenceNormFactorId",
                schema: "Index",
                table: "NormFactor",
                column: "ReferenceNormFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactor_StoneClampRatioId",
                schema: "Index",
                table: "NormFactor",
                column: "StoneClampRatioId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_AssignmentCodeId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "AssignmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_NormFactorAssignmentCode_NormFactorId",
                schema: "Index",
                table: "NormFactorAssignmentCode",
                column: "NormFactorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NormFactorAssignmentCode",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "NormFactor",
                schema: "Index");
        }
    }
}
