using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addFixedKeyDatatable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "MasterData");

            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "QuotaBasedMaterialType",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<Guid>(
                name: "AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialsIncludedInContractRevenueFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuotaBasedMaterialTypeFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FixedKey",
                schema: "MasterData",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FixedKey", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessGroup_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
                column: "FixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemShippedDetail_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail",
                column: "FixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemQuotaBasedMaterialQuantity_FixedKeyId",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity",
                column: "FixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItemIssuedDetail_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail",
                column: "FixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AdditionalCostFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AssetFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_MaterialsIncludedInContractRevenueFixe~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaterialsIncludedInContractRevenueFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "OtherMaterialDetailFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "QuotaBasedMaterialFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_AcceptanceReportItem_QuotaBasedMaterialTypeFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "QuotaBasedMaterialTypeFixedKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_FixedKey_Type_Code",
                schema: "MasterData",
                table: "FixedKey",
                columns: new[] { "Type", "Code" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AdditionalCostFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "AssetFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_MaterialsIncludedInContractRe~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "MaterialsIncludedInContractRevenueFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "OtherMaterialDetailFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "QuotaBasedMaterialFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_QuotaBasedMaterialTypeFixedKe~",
                schema: "Production",
                table: "AcceptanceReportItem",
                column: "QuotaBasedMaterialTypeFixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemIssuedDetail_FixedKey_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail",
                column: "FixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemQuotaBasedMaterialQuantity_FixedKey_Fix~",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity",
                column: "FixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcceptanceReportItemShippedDetail_FixedKey_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail",
                column: "FixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProcessGroup_FixedKey_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup",
                column: "FixedKeyId",
                principalSchema: "MasterData",
                principalTable: "FixedKey",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_MaterialsIncludedInContractRe~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItem_FixedKey_QuotaBasedMaterialTypeFixedKe~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemIssuedDetail_FixedKey_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemQuotaBasedMaterialQuantity_FixedKey_Fix~",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity");

            migrationBuilder.DropForeignKey(
                name: "FK_AcceptanceReportItemShippedDetail_FixedKey_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail");

            migrationBuilder.DropForeignKey(
                name: "FK_ProcessGroup_FixedKey_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropTable(
                name: "FixedKey",
                schema: "MasterData");

            migrationBuilder.DropIndex(
                name: "IX_ProcessGroup_FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemShippedDetail_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemQuotaBasedMaterialQuantity_FixedKeyId",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItemIssuedDetail_FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_MaterialsIncludedInContractRevenueFixe~",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropIndex(
                name: "IX_AcceptanceReportItem_QuotaBasedMaterialTypeFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemShippedDetail");

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                table: "AcceptanceReportItemQuotaBasedMaterialQuantity");

            migrationBuilder.DropColumn(
                name: "FixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItemIssuedDetail");

            migrationBuilder.DropColumn(
                name: "AdditionalCostFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "AssetFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "MaterialsIncludedInContractRevenueFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "OtherMaterialDetailFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "QuotaBasedMaterialFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.DropColumn(
                name: "QuotaBasedMaterialTypeFixedKeyId",
                schema: "Production",
                table: "AcceptanceReportItem");

            migrationBuilder.AlterColumn<int>(
                name: "QuotaBasedMaterialType",
                schema: "Production",
                table: "AcceptanceReportItem",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
