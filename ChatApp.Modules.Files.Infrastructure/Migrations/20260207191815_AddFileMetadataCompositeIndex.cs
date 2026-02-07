using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Files.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileMetadataCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_uploaded_by_created",
                table: "file_metadata",
                columns: new[] { "uploaded_by", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_file_metadata_uploaded_by_created",
                table: "file_metadata");
        }
    }
}
