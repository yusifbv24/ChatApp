using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectConversationLastReadLater : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "User1LastReadLaterAtUtc",
                table: "direct_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "User1LastReadLaterMessageId",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "User2LastReadLaterAtUtc",
                table: "direct_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "User2LastReadLaterMessageId",
                table: "direct_conversations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "User1LastReadLaterAtUtc",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "User1LastReadLaterMessageId",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "User2LastReadLaterAtUtc",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "User2LastReadLaterMessageId",
                table: "direct_conversations");
        }
    }
}
