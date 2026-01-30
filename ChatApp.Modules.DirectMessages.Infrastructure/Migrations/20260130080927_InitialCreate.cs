using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "direct_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user1_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user2_id = table.Column<Guid>(type: "uuid", nullable: false),
                    last_message_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    initiated_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    has_messages = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_notes = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversations", x => x.id);
                });

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
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversation_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_direct_conversation_members_direct_conversations_Conversati~",
                        column: x => x.ConversationId,
                        principalTable: "direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direct_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receiver_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    file_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    edited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplyToMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsForwarded = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PinnedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PinnedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_direct_messages_direct_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "direct_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direct_message_mentions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_message_mentions", x => x.id);
                    table.ForeignKey(
                        name: "FK_direct_message_mentions_direct_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "direct_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direct_message_reactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reaction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_message_reactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_direct_message_reactions_direct_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "direct_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_favorite_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    favorited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_favorite_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_favorite_messages_direct_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "direct_messages",
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

            migrationBuilder.CreateIndex(
                name: "ix_direct_conversations_last_message",
                table: "direct_conversations",
                column: "last_message_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_direct_conversations_user1_id",
                table: "direct_conversations",
                column: "user1_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_conversations_user2_id",
                table: "direct_conversations",
                column: "user2_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_conversations_users",
                table: "direct_conversations",
                columns: new[] { "user1_id", "user2_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_mentions_mentioned_user_id",
                table: "direct_message_mentions",
                column: "mentioned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_mentions_message_id",
                table: "direct_message_mentions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_reactions_message_id",
                table: "direct_message_reactions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_reactions_unique",
                table: "direct_message_reactions",
                columns: new[] { "message_id", "user_id", "reaction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_conversation_created",
                table: "direct_messages",
                columns: new[] { "conversation_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_conversation_id",
                table: "direct_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_receiver_id",
                table: "direct_messages",
                column: "receiver_id");

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_receiver_read",
                table: "direct_messages",
                columns: new[] { "receiver_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_sender_id",
                table: "direct_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_favorite_messages_message_id",
                table: "user_favorite_messages",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_favorite_messages_unique",
                table: "user_favorite_messages",
                columns: new[] { "user_id", "message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_favorite_messages_user_id",
                table: "user_favorite_messages",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_conversation_members");

            migrationBuilder.DropTable(
                name: "direct_message_mentions");

            migrationBuilder.DropTable(
                name: "direct_message_reactions");

            migrationBuilder.DropTable(
                name: "user_favorite_messages");

            migrationBuilder.DropTable(
                name: "direct_messages");

            migrationBuilder.DropTable(
                name: "direct_conversations");
        }
    }
}
