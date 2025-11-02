using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Channels.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialChannelsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "channels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archived_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "channel_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    joined_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    left_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_channel_members_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channel_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    file_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    edited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pinned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    pinned_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_channel_messages_channels_channel_id",
                        column: x => x.channel_id,
                        principalTable: "channels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channel_message_reactions",
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
                    table.PrimaryKey("PK_channel_message_reactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_channel_message_reactions_channel_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "channel_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "channel_message_reads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_channel_message_reads", x => x.id);
                    table.ForeignKey(
                        name: "FK_channel_message_reads_channel_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "channel_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_channel_members_channel_user",
                table: "channel_members",
                columns: new[] { "channel_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_channel_members_is_active",
                table: "channel_members",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_channel_members_user_id",
                table: "channel_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_reactions_message_id",
                table: "channel_message_reactions",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_reactions_unique",
                table: "channel_message_reactions",
                columns: new[] { "message_id", "user_id", "reaction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_reads_message_user",
                table: "channel_message_reads",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_channel_message_reads_user_id",
                table: "channel_message_reads",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_messages_channel_created",
                table: "channel_messages",
                columns: new[] { "channel_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_channel_messages_channel_id",
                table: "channel_messages",
                column: "channel_id");

            migrationBuilder.CreateIndex(
                name: "ix_channel_messages_channel_pinned",
                table: "channel_messages",
                columns: new[] { "channel_id", "is_pinned" });

            migrationBuilder.CreateIndex(
                name: "ix_channel_messages_sender_id",
                table: "channel_messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_channels_created_by",
                table: "channels",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_channels_is_archived",
                table: "channels",
                column: "is_archived");

            migrationBuilder.CreateIndex(
                name: "ix_channels_name",
                table: "channels",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_channels_type",
                table: "channels",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "channel_members");

            migrationBuilder.DropTable(
                name: "channel_message_reactions");

            migrationBuilder.DropTable(
                name: "channel_message_reads");

            migrationBuilder.DropTable(
                name: "channel_messages");

            migrationBuilder.DropTable(
                name: "channels");
        }
    }
}
