using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToDirectConversationMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create new direct_conversation_members table first
            migrationBuilder.CreateTable(
                name: "direct_conversation_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastReadLaterMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsMarkedReadLater = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversation_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_direct_conversation_members_direct_conversations_Conversation~",
                        column: x => x.ConversationId,
                        principalTable: "direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversation_members_ConversationId_UserId",
                table: "direct_conversation_members",
                columns: new[] { "ConversationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversation_members_UserId",
                table: "direct_conversation_members",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_direct_conversation_members_UserId_IsActive",
                table: "direct_conversation_members",
                columns: new[] { "UserId", "IsActive" });

            // Step 2: Data migration - Transfer existing user preferences from DirectConversation to DirectConversationMember
            migrationBuilder.Sql(@"
                -- Create direct_conversation_members records for User1
                INSERT INTO direct_conversation_members (""Id"", ""ConversationId"", ""UserId"", ""IsActive"", ""LastReadLaterMessageId"", ""IsPinned"", ""IsMuted"", ""IsMarkedReadLater"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                SELECT
                    gen_random_uuid(),
                    id,
                    user1_id,
                    is_user1_active,
                    user1_last_read_later_message_id,
                    user1_is_pinned,
                    user1_is_muted,
                    user1_is_marked_read_later,
                    created_at_utc,
                    updated_at_utc
                FROM direct_conversations;

                -- Create direct_conversation_members records for User2 (only if User1Id != User2Id, to avoid duplicate for Notes)
                INSERT INTO direct_conversation_members (""Id"", ""ConversationId"", ""UserId"", ""IsActive"", ""LastReadLaterMessageId"", ""IsPinned"", ""IsMuted"", ""IsMarkedReadLater"", ""CreatedAtUtc"", ""UpdatedAtUtc"")
                SELECT
                    gen_random_uuid(),
                    id,
                    user2_id,
                    is_user2_active,
                    user2_last_read_later_message_id,
                    user2_is_pinned,
                    user2_is_muted,
                    user2_is_marked_read_later,
                    created_at_utc,
                    updated_at_utc
                FROM direct_conversations
                WHERE user1_id != user2_id;
            ");

            // Step 3: Drop old columns from DirectConversation table
            migrationBuilder.DropColumn(
                name: "is_user1_active",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "is_user2_active",
                table: "direct_conversations");

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
                name: "user1_last_read_later_message_id",
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

            migrationBuilder.DropColumn(
                name: "user2_last_read_later_message_id",
                table: "direct_conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_conversation_members");

            migrationBuilder.AddColumn<bool>(
                name: "is_user1_active",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_user2_active",
                table: "direct_conversations",
                type: "boolean",
                nullable: false,
                defaultValue: true);

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

            migrationBuilder.AddColumn<Guid>(
                name: "user1_last_read_later_message_id",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);

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

            migrationBuilder.AddColumn<Guid>(
                name: "user2_last_read_later_message_id",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);
        }
    }
}
