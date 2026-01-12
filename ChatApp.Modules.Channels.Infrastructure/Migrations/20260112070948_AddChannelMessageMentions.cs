using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Channels.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMessageMentions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "channel_message_mentions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mentioned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mentioned_user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_all_mention = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_message_mentions", x => x.id);
                    table.ForeignKey(
                        name: "FK_channel_message_mentions_channel_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "channel_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_mentions_mentioned_user_id",
                table: "channel_message_mentions",
                column: "mentioned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_mentions_message_id",
                table: "channel_message_mentions",
                column: "message_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "channel_message_mentions");
        }
    }
}
