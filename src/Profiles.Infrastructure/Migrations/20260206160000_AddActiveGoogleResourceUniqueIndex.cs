using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Profiles.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddActiveGoogleResourceUniqueIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE UNIQUE INDEX "IX_google_resources_active_team_type"
            ON google_resources("TeamId", "ResourceType")
            WHERE "IsActive" = true AND "TeamId" IS NOT NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP INDEX IF EXISTS "IX_google_resources_active_team_type";
            """);
    }
}
