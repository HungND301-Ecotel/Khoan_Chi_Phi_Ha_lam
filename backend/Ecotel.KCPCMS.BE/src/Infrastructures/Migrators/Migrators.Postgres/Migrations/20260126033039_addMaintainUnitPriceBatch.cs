using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class addMaintainUnitPriceBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaintainUnitPriceBatch",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OtherMaterialValue = table.Column<double>(type: "double precision", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintainUnitPriceBatch", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintainUnitPrice_BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                column: "BatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaintainUnitPrice_MaintainUnitPriceBatch_BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                column: "BatchId",
                principalSchema: "Pricing",
                principalTable: "MaintainUnitPriceBatch",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaintainUnitPrice_MaintainUnitPriceBatch_BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropTable(
                name: "MaintainUnitPriceBatch",
                schema: "Pricing");

            migrationBuilder.DropIndex(
                name: "IX_MaintainUnitPrice_BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "BatchId",
                schema: "Pricing",
                table: "MaintainUnitPrice");
        }
    }
}
