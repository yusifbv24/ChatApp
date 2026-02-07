using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_users_is_active",
                table: "users",
                column: "is_active");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_is_active",
                table: "users");
        }
    }
}
