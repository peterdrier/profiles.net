using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Humans.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAsociadosSystemTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "teams",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "Slug", "SystemTeamType", "UpdatedAt" },
                values: new object[] { new Guid("00000000-0000-0000-0001-000000000004"), NodaTime.Instant.FromUnixTimeTicks(17702491570000000L), "Voting members with approved asociado applications", true, "Asociados", "asociados", "Asociados", NodaTime.Instant.FromUnixTimeTicks(17702491570000000L) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "teams",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0001-000000000004"));
        }
    }
}
