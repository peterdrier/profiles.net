using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Humans.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddApplicationLanguage : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Language",
            table: "applications",
            type: "character varying(10)",
            maxLength: 10,
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Language",
            table: "applications");
    }
}
