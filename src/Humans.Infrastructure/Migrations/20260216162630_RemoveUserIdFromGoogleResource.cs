using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Humans.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromGoogleResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_google_resources_users_UserId",
                table: "google_resources");

            migrationBuilder.DropIndex(
                name: "IX_google_resources_TeamId_GoogleId",
                table: "google_resources");

            migrationBuilder.DropIndex(
                name: "IX_google_resources_UserId",
                table: "google_resources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_google_resources_exactly_one_owner",
                table: "google_resources");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "google_resources");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "google_resources",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_resources_TeamId_GoogleId",
                table: "google_resources",
                columns: new[] { "TeamId", "GoogleId" },
                unique: true,
                filter: "\"IsActive\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_google_resources_TeamId_GoogleId",
                table: "google_resources");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "google_resources",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "google_resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_google_resources_TeamId_GoogleId",
                table: "google_resources",
                columns: new[] { "TeamId", "GoogleId" },
                unique: true,
                filter: "\"IsActive\" = true AND \"TeamId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_google_resources_UserId",
                table: "google_resources",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_google_resources_exactly_one_owner",
                table: "google_resources",
                sql: "(\"TeamId\" IS NOT NULL AND \"UserId\" IS NULL) OR (\"TeamId\" IS NULL AND \"UserId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_google_resources_users_UserId",
                table: "google_resources",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
