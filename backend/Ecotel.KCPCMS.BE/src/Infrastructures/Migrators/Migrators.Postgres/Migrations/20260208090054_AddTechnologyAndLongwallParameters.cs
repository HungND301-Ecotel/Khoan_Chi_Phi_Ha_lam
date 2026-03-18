using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnologyAndLongwallParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LongwallParameters",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Llc = table.Column<string>(type: "text", nullable: false),
                    Lkc = table.Column<double>(type: "double precision", nullable: false),
                    Mk = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LongwallParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Technology",
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
                    table.PrimaryKey("PK_Technology", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TechnologyId");

            migrationBuilder.CreateIndex(
                name: "IX_LongwallParameters_Llc_Lkc_Mk",
                schema: "Index",
                table: "LongwallParameters",
                columns: new[] { "Llc", "Lkc", "Mk" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Technology_Value",
                schema: "Index",
                table: "Technology",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialUnitPrice_Technology_TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "TechnologyId",
                principalSchema: "Index",
                principalTable: "Technology",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialUnitPrice_Technology_TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropTable(
                name: "LongwallParameters",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Technology",
                schema: "Index");

            migrationBuilder.DropIndex(
                name: "IX_MaterialUnitPrice_TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "TechnologyId",
                schema: "Pricing",
                table: "MaterialUnitPrice");
        }
    }
}
