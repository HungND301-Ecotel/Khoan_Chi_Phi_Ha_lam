using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class AddSignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DigitalSignature",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "InitialSignature",
                schema: "Index",
                table: "Employee");

            migrationBuilder.DropColumn(
                name: "StandardSignature",
                schema: "Index",
                table: "Employee");

            migrationBuilder.CreateTable(
                name: "UserSignatures",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SignatureType = table.Column<int>(type: "integer", nullable: false),
                    SignatureFile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificateId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CertificateFile = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PinHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsPinSaved = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserId1 = table.Column<int>(type: "integer", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSignatures_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSignatures_Users_UserId1",
                        column: x => x.UserId1,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_UserId",
                schema: "Identity",
                table: "UserSignatures",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSignatures_UserId1",
                schema: "Identity",
                table: "UserSignatures",
                column: "UserId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSignatures",
                schema: "Identity");

            migrationBuilder.AddColumn<string>(
                name: "DigitalSignature",
                schema: "Index",
                table: "Employee",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialSignature",
                schema: "Index",
                table: "Employee",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StandardSignature",
                schema: "Index",
                table: "Employee",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
