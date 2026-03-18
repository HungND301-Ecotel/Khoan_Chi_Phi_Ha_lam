using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateTimeBaseToMonth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintainUnitPrice_EquipmentId_StartDate_EndDate",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Pricing",
                table: "Output");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Pricing",
                table: "Output");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "EndDate",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "Index",
                table: "Cost");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Pricing",
                table: "SlideUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Pricing",
                table: "SlideUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Pricing",
                table: "Output",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Pricing",
                table: "Output",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndMonth",
                schema: "Index",
                table: "Cost",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartMonth",
                schema: "Index",
                table: "Cost",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_MaintainUnitPrice_EquipmentId_StartMonth_EndMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                columns: new[] { "EquipmentId", "StartMonth", "EndMonth" },
                filter: "\"DeletedOn\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MaintainUnitPrice_EquipmentId_StartMonth_EndMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Pricing",
                table: "SlideUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Pricing",
                table: "Output");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Pricing",
                table: "Output");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Pricing",
                table: "MaterialUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Pricing",
                table: "MaintainUnitPrice");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment");

            migrationBuilder.DropColumn(
                name: "EndMonth",
                schema: "Index",
                table: "Cost");

            migrationBuilder.DropColumn(
                name: "StartMonth",
                schema: "Index",
                table: "Cost");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                schema: "Pricing",
                table: "SlideUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                schema: "Pricing",
                table: "SlideUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                schema: "Pricing",
                table: "Output",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                schema: "Pricing",
                table: "Output",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndDate",
                schema: "Index",
                table: "Cost",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartDate",
                schema: "Index",
                table: "Cost",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_MaintainUnitPrice_EquipmentId_StartDate_EndDate",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                columns: new[] { "EquipmentId", "StartDate", "EndDate" },
                filter: "\"DeletedOn\" IS NULL");
        }
    }
}
