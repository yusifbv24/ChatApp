using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDirectConversationLastReadLaterAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "User1LastReadLaterAtUtc",
                table: "direct_conversations");

            migrationBuilder.DropColumn(
                name: "User2LastReadLaterAtUtc",
                table: "direct_conversations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "User1LastReadLaterAtUtc",
                table: "direct_conversations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "User2LastReadLaterAtUtc",
                table: "direct_conversations",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
