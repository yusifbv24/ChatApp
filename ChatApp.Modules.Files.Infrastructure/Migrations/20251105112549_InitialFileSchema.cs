using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Files.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialFileSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "file_metadata",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    filename = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FileSizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    thumbnail_path = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_metadata", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_created_at",
                table: "file_metadata",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_file_type",
                table: "file_metadata",
                column: "file_type");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_is_deleted",
                table: "file_metadata",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_file_metadata_uploaded_by",
                table: "file_metadata",
                column: "uploaded_by");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_metadata");
        }
    }
}
