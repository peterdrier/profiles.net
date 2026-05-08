using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Humans.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerPlacementNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlacementImageContentType",
                table: "containers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacementImageFileName",
                table: "containers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacementImageStoragePath",
                table: "containers",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlacementNotes",
                table: "containers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlacementImageContentType",
                table: "containers");

            migrationBuilder.DropColumn(
                name: "PlacementImageFileName",
                table: "containers");

            migrationBuilder.DropColumn(
                name: "PlacementImageStoragePath",
                table: "containers");

            migrationBuilder.DropColumn(
                name: "PlacementNotes",
                table: "containers");
        }
    }
}
