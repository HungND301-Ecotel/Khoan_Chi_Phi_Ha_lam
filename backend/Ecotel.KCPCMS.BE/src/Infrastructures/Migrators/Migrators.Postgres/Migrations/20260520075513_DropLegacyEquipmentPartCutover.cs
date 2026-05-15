using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyEquipmentPartCutover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignmentCodeId",
                schema: "Index",
                table: "Cost",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssignmentCodeMaterial",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentCodeMaterial", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentCodeMaterial_AssignmentCode_AssignmentCodeId",
                        column: x => x.AssignmentCodeId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentCodeMaterial_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    });

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cost_OneParentOnly",
                schema: "Index",
                table: "Cost");

            migrationBuilder.Sql("""
                INSERT INTO "Index"."AssignmentCode"
                (
                    "Id",
                    "CodeId",
                    "Name",
                    "UnitOfMeasureId",
                    "IsSlideAssignmentCode",
                    "CreatedBy",
                    "CreatedOn",
                    "LastModifiedBy",
                    "LastModifiedOn",
                    "DeletedOn",
                    "DeletedBy"
                )
                SELECT
                    e."Id",
                    e."CodeId",
                    e."Name",
                    e."UnitOfMeasureId",
                    FALSE,
                    e."CreatedBy",
                    e."CreatedOn",
                    e."LastModifiedBy",
                    e."LastModifiedOn",
                    e."DeletedOn",
                    e."DeletedBy"
                FROM "Index"."Equipment" e
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Index"."AssignmentCode" ac
                    WHERE ac."Id" = e."Id"
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Index"."Material"
                (
                    "Id",
                    "CodeId",
                    "Name",
                    "AssigmentCodeId",
                    "UnitOfMeasureId",
                    "MaterialType",
                    "CreatedBy",
                    "CreatedOn",
                    "LastModifiedBy",
                    "LastModifiedOn",
                    "DeletedOn",
                    "DeletedBy"
                )
                SELECT
                    p."Id",
                    p."CodeId",
                    p."Name",
                    (
                        SELECT ep."EquipmentId"
                        FROM "Index"."EquipmentPart" ep
                        WHERE ep."PartId" = p."Id"
                          AND ep."DeletedOn" IS NULL
                        ORDER BY ep."EquipmentId"
                        LIMIT 1
                    ),
                    p."UnitOfMeasureId",
                    CASE p."Type"
                        WHEN 2 THEN 2
                        ELSE 1
                    END,
                    p."CreatedBy",
                    p."CreatedOn",
                    p."LastModifiedBy",
                    p."LastModifiedOn",
                    p."DeletedOn",
                    p."DeletedBy"
                FROM "Index"."Part" p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Index"."Material" m
                    WHERE m."Id" = p."Id"
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Index"."AssignmentCodeMaterial"
                (
                    "Id",
                    "AssignmentCodeId",
                    "MaterialId",
                    "CreatedBy",
                    "CreatedOn",
                    "LastModifiedBy",
                    "LastModifiedOn",
                    "DeletedOn",
                    "DeletedBy"
                )
                SELECT
                    gen_random_uuid(),
                    ep."EquipmentId",
                    ep."PartId",
                    ep."CreatedBy",
                    ep."CreatedOn",
                    ep."LastModifiedBy",
                    ep."LastModifiedOn",
                    ep."DeletedOn",
                    ep."DeletedBy"
                FROM "Index"."EquipmentPart" ep
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Index"."AssignmentCodeMaterial" acm
                    WHERE acm."AssignmentCodeId" = ep."EquipmentId"
                      AND acm."MaterialId" = ep."PartId"
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO "Index"."AssignmentCodeMaterial"
                (
                    "Id",
                    "AssignmentCodeId",
                    "MaterialId",
                    "CreatedBy",
                    "CreatedOn",
                    "LastModifiedBy",
                    "LastModifiedOn",
                    "DeletedOn",
                    "DeletedBy"
                )
                SELECT
                    gen_random_uuid(),
                    m."AssigmentCodeId",
                    m."Id",
                    m."CreatedBy",
                    m."CreatedOn",
                    m."LastModifiedBy",
                    m."LastModifiedOn",
                    m."DeletedOn",
                    m."DeletedBy"
                FROM "Index"."Material" m
                WHERE m."AssigmentCodeId" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM "Index"."AssignmentCodeMaterial" acm
                      WHERE acm."AssignmentCodeId" = m."AssigmentCodeId"
                        AND acm."MaterialId" = m."Id"
                  );
                """);

            migrationBuilder.Sql("""
                UPDATE "Index"."Cost"
                SET "AssignmentCodeId" = "EquipmentId"
                WHERE "AssignmentCodeId" IS NULL
                  AND "EquipmentId" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Index"."Cost"
                SET "MaterialId" = "PartId"
                WHERE "MaterialId" IS NULL
                  AND "PartId" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Index"."Cost"
                SET "EquipmentId" = NULL
                WHERE "AssignmentCodeId" IS NOT NULL;
                """);

            migrationBuilder.Sql("""
                UPDATE "Index"."Cost"
                SET "PartId" = NULL
                WHERE "MaterialId" IS NOT NULL;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Equipment_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemCategoryAllocationEquipment_Equipment_E~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_ActualEletricityEquipment_Equipment_EquipmentId",
                schema: "Production",
                table: "ActualEletricityEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Cost_Equipment_EquipmentId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropForeignKey(
                name: "FK_Cost_Part_PartId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityUnitPriceEquipment_Equipment_EquipmentId",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_LongTermAnchorSeedItem_Part_PartId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintainUnitPrice_Equipment_EquipmentId",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintainUnitPriceEquipment_Part_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment");

            migrationBuilder.DropTable(
                name: "EquipmentPart",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PartProcessGroup",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Equipment",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Part",
                schema: "Index");

            migrationBuilder.DropIndex(
                name: "IX_Cost_EquipmentId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropIndex(
                name: "IX_Cost_PartId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.CreateIndex(
                name: "IX_Cost_AssignmentCodeId",
                schema: "Index",
                table: "Cost",
                column: "AssignmentCodeId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cost_OneParentOnly",
                schema: "Index",
                table: "Cost",
                sql: "\r\n                    (\r\n                        (CASE WHEN \"MaterialId\"  IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"AssignmentCodeId\" IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"EquipmentId\" IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"PartId\" IS NOT NULL THEN 1 ELSE 0 END)\r\n                    ) = 1\r\n                ");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCodeMaterial_AssignmentCodeId_MaterialId",
                schema: "Index",
                table: "AssignmentCodeMaterial",
                columns: new[] { "AssignmentCodeId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCodeMaterial_MaterialId",
                schema: "Index",
                table: "AssignmentCodeMaterial",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_AssignmentCode_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Material_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemCategoryAllocationEquipment_AssignmentC~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActualEletricityEquipment_AssignmentCode_EquipmentId",
                schema: "Production",
                table: "ActualEletricityEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cost_AssignmentCode_AssignmentCodeId",
                schema: "Index",
                table: "Cost",
                column: "AssignmentCodeId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityUnitPriceEquipment_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermAnchorSeedItem_Material_PartId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintainUnitPrice_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "AssignmentCode",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintainUnitPriceEquipment_Material_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_AssignmentCode_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_Material_PartId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemCategoryAllocationEquipment_AssignmentC~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_ActualEletricityEquipment_AssignmentCode_EquipmentId",
                schema: "Production",
                table: "ActualEletricityEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Cost_AssignmentCode_AssignmentCodeId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropForeignKey(
                name: "FK_ElectricityUnitPriceEquipment_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropForeignKey(
                name: "FK_LongTermAnchorSeedItem_Material_PartId",
                schema: "Production",
                table: "LongTermAnchorSeedItem");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintainUnitPrice_AssignmentCode_EquipmentId",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaintainUnitPriceEquipment_Material_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment");

            migrationBuilder.DropTable(
                name: "AssignmentCodeMaterial",
                schema: "Index");

            migrationBuilder.DropIndex(
                name: "IX_Cost_AssignmentCodeId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Cost_OneParentOnly",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropColumn(
                name: "AssignmentCodeId",
                schema: "Index",
                table: "Cost");

            migrationBuilder.CreateTable(
                name: "Equipment",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Equipment_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Part",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Part", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Part_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Part_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentPart",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentPart", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EquipmentPart_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EquipmentPart_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PartProcessGroup",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartProcessGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartProcessGroup_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PartProcessGroup_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cost_EquipmentId",
                schema: "Index",
                table: "Cost",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cost_PartId",
                schema: "Index",
                table: "Cost",
                column: "PartId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Cost_OneParentOnly",
                schema: "Index",
                table: "Cost",
                sql: "\r\n                    (\r\n                        (CASE WHEN \"MaterialId\"  IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"EquipmentId\" IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"PartId\" IS NOT NULL THEN 1 ELSE 0 END)\r\n                    ) = 1\r\n                ");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_CodeId",
                schema: "Index",
                table: "Equipment",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_UnitOfMeasureId",
                schema: "Index",
                table: "Equipment",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_EquipmentId_PartId",
                schema: "Index",
                table: "EquipmentPart",
                columns: new[] { "EquipmentId", "PartId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentPart_PartId",
                schema: "Index",
                table: "EquipmentPart",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Part_CodeId",
                schema: "Index",
                table: "Part",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Part_UnitOfMeasureId",
                schema: "Index",
                table: "Part",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessGroup_PartId_ProcessGroupId",
                schema: "Index",
                table: "PartProcessGroup",
                columns: new[] { "PartId", "ProcessGroupId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PartProcessGroup_ProcessGroupId",
                schema: "Index",
                table: "PartProcessGroup",
                column: "ProcessGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Equipment_EquipmentId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_Part_PartId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemCategoryAllocationEquipment_Equipment_E~",
                schema: "Production",
                table: "AcceptanceReportItemCategoryAllocationEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ActualEletricityEquipment_Equipment_EquipmentId",
                schema: "Production",
                table: "ActualEletricityEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cost_Equipment_EquipmentId",
                schema: "Index",
                table: "Cost",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Cost_Part_PartId",
                schema: "Index",
                table: "Cost",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ElectricityUnitPriceEquipment_Equipment_EquipmentId",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LongTermAnchorSeedItem_Part_PartId",
                schema: "Production",
                table: "LongTermAnchorSeedItem",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintainUnitPrice_Equipment_EquipmentId",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                column: "EquipmentId",
                principalSchema: "Index",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaintainUnitPriceEquipment_Part_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment",
                column: "PartId",
                principalSchema: "Index",
                principalTable: "Part",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
