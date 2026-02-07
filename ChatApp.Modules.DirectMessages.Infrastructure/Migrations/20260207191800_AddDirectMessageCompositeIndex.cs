using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectMessageCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_conversation_receiver_read",
                table: "direct_messages",
                columns: new[] { "conversation_id", "receiver_id", "is_read" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_direct_messages_conversation_receiver_read",
                table: "direct_messages");
        }
    }
}
