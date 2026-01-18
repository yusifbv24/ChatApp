using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingUserLastReadLaterMessageIdColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "user1_last_read_later_message_id",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user2_last_read_later_message_id",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "user1_last_read_later_message_id",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "user2_last_read_later_message_id",
                table: "direct_conversations");
        }
    }
}
