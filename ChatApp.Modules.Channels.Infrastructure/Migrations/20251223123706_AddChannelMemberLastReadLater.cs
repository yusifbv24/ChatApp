using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Channels.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMemberLastReadLater : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReadLaterAtUtc",
                table: "channel_members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReadLaterMessageId",
                table: "channel_members",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReadLaterAtUtc",
                table: "channel_members");

            migrationBuilder.DropColumn(
                name: "LastReadLaterMessageId",
                table: "channel_members");
        }
    }
}
