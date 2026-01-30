using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Settings.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    push_notifications_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_on_channel_message = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_on_direct_message = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_on_mention = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    notify_on_reaction = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_online_status = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_last_seen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    show_read_receipts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allow_direct_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "light"),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "en"),
                    message_page_size = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");
        }
    }
}
