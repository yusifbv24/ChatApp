using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Modules.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create companies table first
            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    head_of_company_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.ForeignKey(
                        name: "FK_companies_users_head_of_company_id",
                        column: x => x.head_of_company_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_companies_head_of_company_id",
                table: "companies",
                column: "head_of_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_companies_name",
                table: "companies",
                column: "name");

            // Step 2: Insert a default company so existing departments can reference it
            var defaultCompanyId = Guid.NewGuid();
            migrationBuilder.Sql($@"
                INSERT INTO companies (id, name, head_of_company_id, created_at_utc, updated_at_utc)
                VALUES ('{defaultCompanyId}', '166 Logistics', NULL, NOW(), NOW());
            ");

            // Step 3: Add company_id column with the default company ID
            migrationBuilder.AddColumn<Guid>(
                name: "company_id",
                table: "departments",
                type: "uuid",
                nullable: false,
                defaultValue: defaultCompanyId);

            // Step 4: Update existing departments to reference the default company
            migrationBuilder.Sql($@"
                UPDATE departments SET company_id = '{defaultCompanyId}' WHERE company_id = '00000000-0000-0000-0000-000000000000';
            ");

            migrationBuilder.CreateIndex(
                name: "ix_departments_company_id",
                table: "departments",
                column: "company_id");

            // Step 5: Add the foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_departments_companies_company_id",
                table: "departments",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_companies_company_id",
                table: "departments");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropIndex(
                name: "ix_departments_company_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "company_id",
                table: "departments");
        }
    }
}
