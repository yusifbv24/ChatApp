using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChatApp.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    module = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "id", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestampt with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "id", nullable: false),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    avatar_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "id", nullable: false),
                    permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "id", nullable: false),
                    token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "id", nullable: false),
                    role_id = table.Column<Guid>(type: "id", nullable: false),
                    assigned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "Id", "created_at_utc", "description", "module", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(729), "Create users", "Identity", "Users.Create", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(864) },
                    { new Guid("11111111-1111-1111-1111-111111111112"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1332), "Read users", "Identity", "Users.Read", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1332) },
                    { new Guid("11111111-1111-1111-1111-111111111113"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1336), "Update users", "Identity", "Users.Update", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1336) },
                    { new Guid("11111111-1111-1111-1111-111111111114"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1338), "Delete users", "Identity", "Users.Delete", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1339) },
                    { new Guid("11111111-1111-1111-1111-111111111115"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1340), "Create roles", "Identity", "Roles.Create", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1341) },
                    { new Guid("11111111-1111-1111-1111-111111111116"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1342), "Read roles", "Identity", "Roles.Read", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1343) },
                    { new Guid("11111111-1111-1111-1111-111111111117"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1344), "Update roles", "Identity", "Roles.Update", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1345) },
                    { new Guid("11111111-1111-1111-1111-111111111118"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1347), "Delete roles", "Identity", "Roles.Delete", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1347) },
                    { new Guid("11111111-1111-1111-1111-111111111119"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1352), "Send messages", "Messaging", "Messages.Send", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1352) },
                    { new Guid("11111111-1111-1111-1111-11111111111a"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1354), "Read messages", "Messaging", "Messages.Read", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1354) },
                    { new Guid("11111111-1111-1111-1111-11111111111b"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1356), "Edit messages", "Messaging", "Messages.Edit", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1356) },
                    { new Guid("11111111-1111-1111-1111-11111111111c"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1358), "Delete messages", "Messaging", "Messages.Delete", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1358) },
                    { new Guid("11111111-1111-1111-1111-11111111111d"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1360), "Upload files", "Files", "Files.Upload", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1360) },
                    { new Guid("11111111-1111-1111-1111-11111111111e"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1362), "Download files", "Files", "Files.Download", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1362) },
                    { new Guid("11111111-1111-1111-1111-11111111111f"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1364), "Delete files", "Files", "Files.Delete", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1364) },
                    { new Guid("11111111-1111-1111-1111-111111111120"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1365), "Create groups", "Messaging", "Groups.Create", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1366) },
                    { new Guid("11111111-1111-1111-1111-111111111121"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1369), "Manage groups", "Messaging", "Groups.Manage", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(1369) }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "Id", "created_at_utc", "description", "is_system_role", "name", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222221"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(5782), "Basic user role", true, "User", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(5783) },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(6138), "Operator role with extended permissions", true, "Operator", new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(6139) }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "id", "created_at_utc", "permission_id", "role_id", "updated_at_utc" },
                values: new object[,]
                {
                    { new Guid("104a375d-1b29-465d-95db-75e4eaa7995c"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7078), new Guid("11111111-1111-1111-1111-11111111111e"), new Guid("22222222-2222-2222-2222-222222222221"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7078) },
                    { new Guid("463ce3b7-0c45-4ed5-853d-f9eb64f12efa"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7070), new Guid("11111111-1111-1111-1111-11111111111d"), new Guid("22222222-2222-2222-2222-222222222221"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7070) },
                    { new Guid("9ad6ac5a-7772-4a77-8667-464e13f11f80"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(6805), new Guid("11111111-1111-1111-1111-111111111119"), new Guid("22222222-2222-2222-2222-222222222221"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(6806) },
                    { new Guid("ae398ecf-82cd-48ef-82a3-535506c96142"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7067), new Guid("11111111-1111-1111-1111-11111111111a"), new Guid("22222222-2222-2222-2222-222222222221"), new DateTime(2025, 11, 1, 8, 41, 28, 817, DateTimeKind.Utc).AddTicks(7068) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_permissions_module",
                table: "permissions",
                column: "module");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_name",
                table: "permissions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id_role_id",
                table: "user_roles",
                columns: new[] { "user_id", "role_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_display_name",
                table: "users",
                column: "display_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_id",
                table: "users",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
