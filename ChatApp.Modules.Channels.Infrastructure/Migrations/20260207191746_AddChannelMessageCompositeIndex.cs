using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Channels.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChannelMessageCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_channel_messages_channel_deleted_created",
                table: "channel_messages",
                columns: new[] { "channel_id", "is_deleted", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_channel_messages_channel_deleted_created",
                table: "channel_messages");
        }
    }
}
