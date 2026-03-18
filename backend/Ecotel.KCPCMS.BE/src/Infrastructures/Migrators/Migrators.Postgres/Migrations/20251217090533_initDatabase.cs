using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Migrators.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class initDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Pricing");

            migrationBuilder.EnsureSchema(
                name: "Index");

            migrationBuilder.EnsureSchema(
                name: "Auditing");

            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.CreateTable(
                name: "AuditTrails",
                schema: "Auditing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: true),
                    TableName = table.Column<string>(type: "text", nullable: true),
                    DateTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OldValues = table.Column<string>(type: "text", nullable: true),
                    NewValues = table.Column<string>(type: "text", nullable: true),
                    AffectedColumns = table.Column<string>(type: "text", nullable: true),
                    PrimaryKey = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditTrails", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Code",
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
                    table.PrimaryKey("PK_Code", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hardness",
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
                    table.PrimaryKey("PK_Hardness", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsertItem",
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
                    table.PrimaryKey("PK_InsertItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Passport",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Sd = table.Column<string>(type: "text", nullable: false),
                    Sc = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Passport", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RoleType = table.Column<int>(type: "integer", nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportStep",
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
                    table.PrimaryKey("PK_SupportStep", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitOfMeasure",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOfMeasure", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Avatar = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    JoinDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Fullname = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsVerifiedPhone = table.Column<bool>(type: "boolean", nullable: true),
                    IsVerifiedEmail = table.Column<bool>(type: "boolean", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordResetExpiration = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegisterProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Province = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    District = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Ward = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StreetAddress = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Dob = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<bool>(type: "boolean", nullable: true),
                    Job = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Cccd = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserVerifications",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Mode = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenExpirationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    VerificationCode = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    CodeExpirationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    DeletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVerifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessGroup",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessGroup_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentCode",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentCode_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssignmentCode_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
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
                name: "RefreshTokens",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiredDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: false),
                    ClaimValue = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    RoleType = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdjustmentFactor",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdjustmentFactor_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdjustmentFactor_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Product",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Product", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Product_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Product_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionProcess",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionProcess", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionProcess_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionProcess_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlideUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardnessId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlideUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlideUnitPrice_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlideUnitPrice_Hardness_HardnessId",
                        column: x => x.HardnessId,
                        principalSchema: "Index",
                        principalTable: "Hardness",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlideUnitPrice_Passport_PassportId",
                        column: x => x.PassportId,
                        principalSchema: "Index",
                        principalTable: "Passport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlideUnitPrice_ProcessGroup_ProcessGroupId",
                        column: x => x.ProcessGroupId,
                        principalSchema: "Index",
                        principalTable: "ProcessGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Material",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AssigmentCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialType = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Material_AssignmentCode_AssigmentCodeId",
                        column: x => x.AssigmentCodeId,
                        principalSchema: "Index",
                        principalTable: "AssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Material_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Material_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ElectricityUnitPriceEquipment",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MonthlyElectricityCost = table.Column<double>(type: "double precision", nullable: false),
                    AverageMonthlyTunnelProduction = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectricityUnitPriceEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectricityUnitPriceEquipment_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintainUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintainUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintainUnitPrice_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
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
                    Name = table.Column<string>(type: "text", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
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
                        name: "FK_Part_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
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
                name: "AdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    AdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceAdjustmentValue = table.Column<double>(type: "double precision", nullable: true),
                    ElectricityAdjustmentValue = table.Column<double>(type: "double precision", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdjustmentFactorDescription_AdjustmentFactor_AdjustmentFact~",
                        column: x => x.AdjustmentFactorId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitOfMeasureId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductUnitPrice_Product_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "Index",
                        principalTable: "Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductUnitPrice_UnitOfMeasure_UnitOfMeasureId",
                        column: x => x.UnitOfMeasureId,
                        principalSchema: "Index",
                        principalTable: "UnitOfMeasure",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialUnitPrice",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassportId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardnessId = table.Column<Guid>(type: "uuid", nullable: false),
                    InsertItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupportStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AddOtherMaterialUnitPrice = table.Column<bool>(type: "boolean", nullable: false),
                    OtherMaterialValue = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialUnitPrice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_Code_CodeId",
                        column: x => x.CodeId,
                        principalSchema: "Index",
                        principalTable: "Code",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_Hardness_HardnessId",
                        column: x => x.HardnessId,
                        principalSchema: "Index",
                        principalTable: "Hardness",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_InsertItem_InsertItemId",
                        column: x => x.InsertItemId,
                        principalSchema: "Index",
                        principalTable: "InsertItem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_Passport_PassportId",
                        column: x => x.PassportId,
                        principalSchema: "Index",
                        principalTable: "Passport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_ProductionProcess_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPrice_SupportStep_SupportStepId",
                        column: x => x.SupportStepId,
                        principalSchema: "Index",
                        principalTable: "SupportStep",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoneClampRatio",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    CoefficientValue = table.Column<double>(type: "double precision", nullable: false),
                    HardnessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoneClampRatio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoneClampRatio_Hardness_HardnessId",
                        column: x => x.HardnessId,
                        principalSchema: "Index",
                        principalTable: "Hardness",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoneClampRatio_ProductionProcess_ProcessId",
                        column: x => x.ProcessId,
                        principalSchema: "Index",
                        principalTable: "ProductionProcess",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlideUnitPriceAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SlideUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlideUnitPriceAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlideUnitPriceAssignmentCode_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SlideUnitPriceAssignmentCode_SlideUnitPrice_SlideUnitPriceId",
                        column: x => x.SlideUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "SlideUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cost",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CostType = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: true),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: true),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cost", x => x.Id);
                    table.CheckConstraint("CK_Cost_OneParentOnly", "\r\n                    (\r\n                        (CASE WHEN \"MaterialId\"  IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"EquipmentId\" IS NOT NULL THEN 1 ELSE 0 END) +\r\n                        (CASE WHEN \"PartId\" IS NOT NULL THEN 1 ELSE 0 END)\r\n                    ) = 1\r\n                ");
                    table.ForeignKey(
                        name: "FK_Cost_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalSchema: "Index",
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cost_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cost_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintainUnitPriceEquipment",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintainUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    ReplacementTimeStandard = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AverageMonthlyTunnelProduction = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintainUnitPriceEquipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintainUnitPriceEquipment_MaintainUnitPrice_MaintainUnitPr~",
                        column: x => x.MaintainUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaintainUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintainUnitPriceEquipment_Part_PartId",
                        column: x => x.PartId,
                        principalSchema: "Index",
                        principalTable: "Part",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Output",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionMeters = table.Column<double>(type: "double precision", nullable: false),
                    OutputType = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Output", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Output_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialUnitPriceAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_MaterialUnitPrice_MaterialU~",
                        column: x => x.MaterialUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaterialUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialUnitPriceAssignmentCode_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualElectricityCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualElectricityCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaterialCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaterialCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedElectricityCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedElectricityCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedMaintainCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedMaintainCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedMaterialCost",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlideUnitPriceAssignmentCodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    StoneClampRatioId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedMaterialCost", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedMaterialCost_MaterialUnitPrice_MaterialUnitPriceId",
                        column: x => x.MaterialUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaterialUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaterialCost_Output_OutputId",
                        column: x => x.OutputId,
                        principalSchema: "Pricing",
                        principalTable: "Output",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaterialCost_ProductUnitPrice_ProductUnitPriceId",
                        column: x => x.ProductUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ProductUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaterialCost_SlideUnitPriceAssignmentCode_SlideUnitP~",
                        column: x => x.SlideUnitPriceAssignmentCodeId,
                        principalSchema: "Pricing",
                        principalTable: "SlideUnitPriceAssignmentCode",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaterialCost_StoneClampRatio_StoneClampRatioId",
                        column: x => x.StoneClampRatioId,
                        principalSchema: "Index",
                        principalTable: "StoneClampRatio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualElectricityCostAdjustmentFactor",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualElectricityCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectricityUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualElectricityCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactor_ActualElectricityCost~",
                        column: x => x.ActualElectricityCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualElectricityCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactor_ElectricityUnitPriceE~",
                        column: x => x.ElectricityUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ElectricityUnitPriceEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCostAdjustmentFactor",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaintainCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintainUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactor_ActualMaintainCost_Actua~",
                        column: x => x.ActualMaintainCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaintainCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactor_MaintainUnitPrice_Mainta~",
                        column: x => x.MaintainUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaintainUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaterialCostAssignmentCode",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaterialCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaterialCostAssignmentCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCostAssignmentCode_ActualMaterialCost_ActualM~",
                        column: x => x.ActualMaterialCostId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaterialCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaterialCostAssignmentCode_Material_MaterialId",
                        column: x => x.MaterialId,
                        principalSchema: "Index",
                        principalTable: "Material",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedElectricityCostAdjustmentFactor",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedElectricityCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ElectricityUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedElectricityCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCostAdjustmentFactor_ElectricityUnitPrice~",
                        column: x => x.ElectricityUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "ElectricityUnitPriceEquipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCostAdjustmentFactor_PlannedElectricityCo~",
                        column: x => x.PlannedElectricityCostId,
                        principalSchema: "Pricing",
                        principalTable: "PlannedElectricityCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedMaintainCostAdjustmentFactor",
                schema: "Pricing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedMaintainCostId = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintainUnitPriceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedMaintainCostAdjustmentFactor", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCostAdjustmentFactor_MaintainUnitPrice_Maint~",
                        column: x => x.MaintainUnitPriceId,
                        principalSchema: "Pricing",
                        principalTable: "MaintainUnitPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCostAdjustmentFactor_PlannedMaintainCost_Pla~",
                        column: x => x.PlannedMaintainCostId,
                        principalSchema: "Pricing",
                        principalTable: "PlannedMaintainCost",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualElectricityCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualElectricityCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualElectricityCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactorDescription_ActualElec~",
                        column: x => x.ActualElectricityCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "ActualElectricityCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualElectricityCostAdjustmentFactorDescription_Adjustment~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActualMaintainCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualMaintainCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActualMaintainCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactorDescription_ActualMaintai~",
                        column: x => x.ActualMaintainCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "ActualMaintainCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActualMaintainCostAdjustmentFactorDescription_AdjustmentFac~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedElectricityCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedElectricityCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedElectricityCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCostAdjustmentFactorDescription_Adjustmen~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedElectricityCostAdjustmentFactorDescription_PlannedEl~",
                        column: x => x.PlannedElectricityCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "PlannedElectricityCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlannedMaintainCostAdjustmentFactorDescription",
                schema: "Index",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedMaintainCostAdjustmentFactorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdjustmentFactorDescriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedBy = table.Column<long>(type: "bigint", nullable: false),
                    LastModifiedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlannedMaintainCostAdjustmentFactorDescription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentFa~",
                        column: x => x.AdjustmentFactorDescriptionId,
                        principalSchema: "Index",
                        principalTable: "AdjustmentFactorDescription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlannedMaintainCostAdjustmentFactorDescription_PlannedMaint~",
                        column: x => x.PlannedMaintainCostAdjustmentFactorId,
                        principalSchema: "Pricing",
                        principalTable: "PlannedMaintainCostAdjustmentFactor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCost_OutputId",
                schema: "Pricing",
                table: "ActualElectricityCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualElectricityCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactor_ActualElectricityCost~",
                schema: "Pricing",
                table: "ActualElectricityCostAdjustmentFactor",
                column: "ActualElectricityCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactor_ElectricityUnitPriceId",
                schema: "Pricing",
                table: "ActualElectricityCostAdjustmentFactor",
                column: "ElectricityUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactorDescription_ActualElec~",
                schema: "Index",
                table: "ActualElectricityCostAdjustmentFactorDescription",
                column: "ActualElectricityCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualElectricityCostAdjustmentFactorDescription_Adjustment~",
                schema: "Index",
                table: "ActualElectricityCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCost_OutputId",
                schema: "Pricing",
                table: "ActualMaintainCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualMaintainCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactor_ActualMaintainCostId",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor",
                column: "ActualMaintainCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactor_MaintainUnitPriceId",
                schema: "Pricing",
                table: "ActualMaintainCostAdjustmentFactor",
                column: "MaintainUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactorDescription_ActualMaintai~",
                schema: "Index",
                table: "ActualMaintainCostAdjustmentFactorDescription",
                column: "ActualMaintainCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaintainCostAdjustmentFactorDescription_AdjustmentFac~",
                schema: "Index",
                table: "ActualMaintainCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCost_OutputId",
                schema: "Pricing",
                table: "ActualMaterialCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "ActualMaterialCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCostAssignmentCode_ActualMaterialCostId",
                schema: "Pricing",
                table: "ActualMaterialCostAssignmentCode",
                column: "ActualMaterialCostId");

            migrationBuilder.CreateIndex(
                name: "IX_ActualMaterialCostAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "ActualMaterialCostAssignmentCode",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentFactor_CodeId",
                schema: "Index",
                table: "AdjustmentFactor",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentFactor_ProcessGroupId",
                schema: "Index",
                table: "AdjustmentFactor",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AdjustmentFactorDescription_AdjustmentFactorId",
                schema: "Index",
                table: "AdjustmentFactorDescription",
                column: "AdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCode_CodeId",
                schema: "Index",
                table: "AssignmentCode",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentCode_UnitOfMeasureId",
                schema: "Index",
                table: "AssignmentCode",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_Code_Value",
                schema: "Index",
                table: "Code",
                column: "Value",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Cost_EquipmentId",
                schema: "Index",
                table: "Cost",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cost_MaterialId",
                schema: "Index",
                table: "Cost",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Cost_PartId",
                schema: "Index",
                table: "Cost",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectricityUnitPriceEquipment_EquipmentId",
                schema: "Pricing",
                table: "ElectricityUnitPriceEquipment",
                column: "EquipmentId",
                filter: "\"DeletedOn\" IS NULL");

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
                name: "IX_MaintainUnitPrice_EquipmentId_StartDate_EndDate",
                schema: "Pricing",
                table: "MaintainUnitPrice",
                columns: new[] { "EquipmentId", "StartDate", "EndDate" },
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintainUnitPriceEquipment_MaintainUnitPriceId_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment",
                columns: new[] { "MaintainUnitPriceId", "PartId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaintainUnitPriceEquipment_PartId",
                schema: "Pricing",
                table: "MaintainUnitPriceEquipment",
                column: "PartId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_AssigmentCodeId",
                schema: "Index",
                table: "Material",
                column: "AssigmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Material_CodeId",
                schema: "Index",
                table: "Material",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Material_UnitOfMeasureId",
                schema: "Index",
                table: "Material",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_CodeId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_HardnessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "HardnessId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_InsertItemId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "InsertItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_PassportId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "PassportId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_ProcessId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPrice_SupportStepId",
                schema: "Pricing",
                table: "MaterialUnitPrice",
                column: "SupportStepId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialUnitPriceAssignmentCode_MaterialUnitPriceId_Materia~",
                schema: "Pricing",
                table: "MaterialUnitPriceAssignmentCode",
                columns: new[] { "MaterialUnitPriceId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Output_ProductUnitPriceId",
                schema: "Pricing",
                table: "Output",
                column: "ProductUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_Part_CodeId",
                schema: "Index",
                table: "Part",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Part_EquipmentId",
                schema: "Index",
                table: "Part",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Part_UnitOfMeasureId",
                schema: "Index",
                table: "Part",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCost_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "PlannedElectricityCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactor_ElectricityUnitPrice~",
                schema: "Pricing",
                table: "PlannedElectricityCostAdjustmentFactor",
                column: "ElectricityUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactor_PlannedElectricityCo~",
                schema: "Pricing",
                table: "PlannedElectricityCostAdjustmentFactor",
                column: "PlannedElectricityCostId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactorDescription_Adjustmen~",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedElectricityCostAdjustmentFactorDescription_PlannedEl~",
                schema: "Index",
                table: "PlannedElectricityCostAdjustmentFactorDescription",
                column: "PlannedElectricityCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "PlannedMaintainCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactor_MaintainUnitPriceId",
                schema: "Pricing",
                table: "PlannedMaintainCostAdjustmentFactor",
                column: "MaintainUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactor_PlannedMaintainCostId",
                schema: "Pricing",
                table: "PlannedMaintainCostAdjustmentFactor",
                column: "PlannedMaintainCostId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactorDescription_AdjustmentFa~",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                column: "AdjustmentFactorDescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaintainCostAdjustmentFactorDescription_PlannedMaint~",
                schema: "Index",
                table: "PlannedMaintainCostAdjustmentFactorDescription",
                column: "PlannedMaintainCostAdjustmentFactorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_MaterialUnitPriceId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "MaterialUnitPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "OutputId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_ProductUnitPriceId_OutputId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                columns: new[] { "ProductUnitPriceId", "OutputId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_SlideUnitPriceAssignmentCodeId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "SlideUnitPriceAssignmentCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PlannedMaterialCost_StoneClampRatioId",
                schema: "Pricing",
                table: "PlannedMaterialCost",
                column: "StoneClampRatioId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessGroup_CodeId",
                schema: "Index",
                table: "ProcessGroup",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_CodeId",
                schema: "Index",
                table: "Product",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Product_ProcessGroupId",
                schema: "Index",
                table: "Product",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProcess_CodeId",
                schema: "Index",
                table: "ProductionProcess",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionProcess_ProcessGroupId",
                schema: "Index",
                table: "ProductionProcess",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_ProductId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "ProductId",
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductUnitPrice_UnitOfMeasureId",
                schema: "Pricing",
                table: "ProductUnitPrice",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                schema: "Identity",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_CodeId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "CodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_HardnessId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "HardnessId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_PassportId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "PassportId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPrice_ProcessGroupId",
                schema: "Pricing",
                table: "SlideUnitPrice",
                column: "ProcessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPriceAssignmentCode_MaterialId",
                schema: "Pricing",
                table: "SlideUnitPriceAssignmentCode",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SlideUnitPriceAssignmentCode_SlideUnitPriceId_MaterialId",
                schema: "Pricing",
                table: "SlideUnitPriceAssignmentCode",
                columns: new[] { "SlideUnitPriceId", "MaterialId" },
                unique: true,
                filter: "\"DeletedOn\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StoneClampRatio_HardnessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "HardnessId");

            migrationBuilder.CreateIndex(
                name: "IX_StoneClampRatio_ProcessId",
                schema: "Index",
                table: "StoneClampRatio",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                schema: "Identity",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "Identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                schema: "Identity",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_Email",
                schema: "Identity",
                table: "UserVerifications",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_Phone",
                schema: "Identity",
                table: "UserVerifications",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_Token",
                schema: "Identity",
                table: "UserVerifications",
                column: "Token");

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_VerificationCode",
                schema: "Identity",
                table: "UserVerifications",
                column: "VerificationCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActualElectricityCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ActualMaintainCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ActualMaterialCostAssignmentCode",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "AuditTrails",
                schema: "Auditing");

            migrationBuilder.DropTable(
                name: "Cost",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "MaintainUnitPriceEquipment",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "MaterialUnitPriceAssignmentCode",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "PlannedElectricityCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PlannedMaintainCostAdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PlannedMaterialCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "RefreshTokens",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserClaims",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "UserVerifications",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ActualElectricityCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaintainCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaterialCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "Part",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PlannedElectricityCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "AdjustmentFactorDescription",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "PlannedMaintainCostAdjustmentFactor",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "MaterialUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "SlideUnitPriceAssignmentCode",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "StoneClampRatio",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "ActualElectricityCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ActualMaintainCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ElectricityUnitPriceEquipment",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "PlannedElectricityCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "AdjustmentFactor",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "MaintainUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "PlannedMaintainCost",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "InsertItem",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "SupportStep",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Material",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "SlideUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "ProductionProcess",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Equipment",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Output",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "AssignmentCode",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Hardness",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Passport",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ProductUnitPrice",
                schema: "Pricing");

            migrationBuilder.DropTable(
                name: "Product",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "UnitOfMeasure",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "ProcessGroup",
                schema: "Index");

            migrationBuilder.DropTable(
                name: "Code",
                schema: "Index");
        }
    }
}
