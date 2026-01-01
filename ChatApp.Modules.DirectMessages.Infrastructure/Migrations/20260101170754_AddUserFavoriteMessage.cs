using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.DirectMessages.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFavoriteMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "user_favorite_messages");
        }
    }
}
