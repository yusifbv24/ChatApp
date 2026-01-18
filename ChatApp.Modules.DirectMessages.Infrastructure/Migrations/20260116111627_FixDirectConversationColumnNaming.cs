using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDirectConversationColumnNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsNotes",
                table: "direct_conversations",
                newName: "is_notes");

            migrationBuilder.RenameColumn(
                name: "HasMessages",
                table: "direct_conversations",
                newName: "has_messages");

            migrationBuilder.RenameColumn(
                name: "InitiatedByUserId",
                table: "direct_conversations",
                newName: "initiated_by_user_id");

            migrationBuilder.RenameColumn(
                name: "User1LastReadLaterMessageId",
                table: "direct_conversations",
                newName: "user1_last_read_later_message_id");

            migrationBuilder.RenameColumn(
                name: "User2LastReadLaterMessageId",
                table: "direct_conversations",
                newName: "user2_last_read_later_message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_notes",
                table: "direct_conversations",
                newName: "IsNotes");

            migrationBuilder.RenameColumn(
                name: "has_messages",
                table: "direct_conversations",
                newName: "HasMessages");

            migrationBuilder.RenameColumn(
                name: "initiated_by_user_id",
                table: "direct_conversations",
                newName: "InitiatedByUserId");

            migrationBuilder.RenameColumn(
                name: "user1_last_read_later_message_id",
                table: "direct_conversations",
                newName: "User1LastReadLaterMessageId");

            migrationBuilder.RenameColumn(
                name: "user2_last_read_later_message_id",
                table: "direct_conversations",
                newName: "User2LastReadLaterMessageId");
        }
    }
}
