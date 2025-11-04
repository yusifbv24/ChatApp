using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDirectMessagesSchema : Migration
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
                    is_user1_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_user2_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_direct_conversations", x => x.id);
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
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_message_reactions");

            migrationBuilder.DropTable(
                name: "direct_messages");

            migrationBuilder.DropTable(
                name: "direct_conversations");
        }
    }
}
