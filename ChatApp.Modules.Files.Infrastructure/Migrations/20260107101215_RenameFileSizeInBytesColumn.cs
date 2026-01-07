using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Files.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameFileSizeInBytesColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileSizeInBytes",
                table: "file_metadata",
                newName: "file_size_in_bytes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "file_size_in_bytes",
                table: "file_metadata",
                newName: "FileSizeInBytes");
        }
    }
}
