using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddNoteAndCodeToCatalogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "UnitOfMeasure",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Technology",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "SupportStep",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "StoneClampRatio",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "SeamFace",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "ProductionProcess",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "ProductionOrder",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Product",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "ProcessGroup",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Power",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Index",
                table: "Position",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "Index",
                table: "Position",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Passport",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "NormFactor",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Material",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "LongwallParameters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "InsertItem",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "HaulDistance",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Hardness",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                schema: "Index",
                table: "Employee",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Employee",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "Department",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "CuttingThickness",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "AssignmentCode",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "AdjustmentFactorDescription",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                schema: "Index",
                table: "AdjustmentFactor",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "UnitOfMeasure");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Technology");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "SupportStep");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "StoneClampRatio");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "SeamFace");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "ProductionProcess");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "ProductionOrder");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "ProcessGroup");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Power");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "Index",
                table: "Position");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Passport");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "NormFactor");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Material");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "LongwallParameters");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "InsertItem");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "HaulDistance");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Hardness");

            migrationBuilder.DropColumn(
                name: "Code",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "Department");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "CuttingThickness");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "AssignmentCode");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "AdjustmentFactorDescription");

            migrationBuilder.DropColumn(
                name: "Note",
                schema: "Index",
                table: "AdjustmentFactor");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "Index",
                table: "Position",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
