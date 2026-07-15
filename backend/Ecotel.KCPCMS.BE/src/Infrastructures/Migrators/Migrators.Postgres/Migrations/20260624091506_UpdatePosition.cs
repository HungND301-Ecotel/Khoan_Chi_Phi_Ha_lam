using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSignatures_Users_UserId1",
                schema: "Identity",
                table: "UserSignatures");

            migrationBuilder.DropIndex(
                name: "IX_UserSignatures_UserId1",
                schema: "Identity",
                table: "UserSignatures");

            migrationBuilder.DropColumn(
                name: "UserId1",
                schema: "Identity",
                table: "UserSignatures");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "Index",
                table: "Position",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Level",
                schema: "Index",
                table: "Position",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                schema: "Index",
                table: "Position");

            migrationBuilder.DropColumn(
                name: "Level",
                schema: "Index",
                table: "Position");

            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                schema: "Identity",
                table: "UserSignatures",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_UserId1",
                schema: "Identity",
                table: "UserSignatures",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSignatures_Users_UserId1",
                schema: "Identity",
                table: "UserSignatures",
                column: "UserId1",
                principalSchema: "Identity",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
