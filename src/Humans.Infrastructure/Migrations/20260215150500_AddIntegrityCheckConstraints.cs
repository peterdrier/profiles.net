using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Humans.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrityCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_google_resources_exactly_one_owner",
                table: "google_resources",
                sql: "(\"TeamId\" IS NOT NULL AND \"UserId\" IS NULL) OR (\"TeamId\" IS NULL AND \"UserId\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_role_assignments_valid_window",
                table: "role_assignments",
                sql: "\"ValidTo\" IS NULL OR \"ValidTo\" > \"ValidFrom\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_google_resources_exactly_one_owner",
                table: "google_resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_role_assignments_valid_window",
                table: "role_assignments");
        }
    }
}
