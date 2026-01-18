using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "user1_is_marked_read_later",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "user1_is_muted",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "user1_is_pinned",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "user2_is_marked_read_later",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "user2_is_muted",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "user2_is_pinned",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user1_is_marked_read_later",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user1_is_muted",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user1_is_pinned",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user2_is_marked_read_later",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user2_is_muted",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user2_is_pinned",
                table: "direct_conversations");
        }
    }
}
