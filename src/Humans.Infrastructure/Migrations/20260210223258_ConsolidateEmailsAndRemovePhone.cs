using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Humans.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEmailsAndRemovePhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create user_emails table
            migrationBuilder.CreateTable(
                name: "user_emails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    IsOAuth = table.Column<bool>(type: "boolean", nullable: false),
                    IsNotificationTarget = table.Column<bool>(type: "boolean", nullable: false),
                    Visibility = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VerificationSentAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_emails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_emails_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_emails_UserId",
                table: "user_emails",
                column: "UserId");

            // Step 2: Migrate OAuth email for every user (User.Email → UserEmail with IsOAuth=true)
            // Initially set IsNotificationTarget=true for everyone; step 3 will update for those with verified preferred email
            migrationBuilder.Sql(@"
                INSERT INTO user_emails (""Id"", ""UserId"", ""Email"", ""IsVerified"", ""IsOAuth"", ""IsNotificationTarget"", ""Visibility"", ""VerificationSentAt"", ""DisplayOrder"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), ""Id"", ""Email"", true, true, true, NULL, NULL, 0, now(), now()
                FROM users
                WHERE ""Email"" IS NOT NULL;
            ");

            // Step 3: For users with a verified PreferredEmail, insert as additional UserEmail and make it the notification target
            migrationBuilder.Sql(@"
                INSERT INTO user_emails (""Id"", ""UserId"", ""Email"", ""IsVerified"", ""IsOAuth"", ""IsNotificationTarget"", ""Visibility"", ""VerificationSentAt"", ""DisplayOrder"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), ""Id"", ""PreferredEmail"", true, false, true, NULL, NULL, 1, now(), now()
                FROM users
                WHERE ""PreferredEmailVerified"" = true AND ""PreferredEmail"" IS NOT NULL;

                -- Set OAuth email's IsNotificationTarget to false for these users
                UPDATE user_emails SET ""IsNotificationTarget"" = false
                WHERE ""IsOAuth"" = true
                AND ""UserId"" IN (
                    SELECT ""Id"" FROM users
                    WHERE ""PreferredEmailVerified"" = true AND ""PreferredEmail"" IS NOT NULL
                );
            ");

            // Step 4: Migrate ContactField rows with FieldType='Email' to unverified UserEmail rows
            // User must re-verify these after migration
            // Note: FieldType and Visibility are stored as strings (EF HasConversion<string>)
            migrationBuilder.Sql(@"
                INSERT INTO user_emails (""Id"", ""UserId"", ""Email"", ""IsVerified"", ""IsOAuth"", ""IsNotificationTarget"", ""Visibility"", ""VerificationSentAt"", ""DisplayOrder"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), p.""UserId"", cf.""Value"", false, false, false,
                    cf.""Visibility"",
                    NULL, cf.""DisplayOrder"" + 10, cf.""CreatedAt"", cf.""UpdatedAt""
                FROM contact_fields cf
                INNER JOIN profiles p ON cf.""ProfileId"" = p.""Id""
                WHERE cf.""FieldType"" = 'Email'
                AND NOT EXISTS (
                    -- Skip if this email already exists for the user (e.g. same as OAuth email)
                    SELECT 1 FROM user_emails ue
                    WHERE ue.""UserId"" = p.""UserId"" AND LOWER(ue.""Email"") = LOWER(cf.""Value"")
                );
            ");

            // Step 5: Delete ContactField rows with FieldType='Email'
            migrationBuilder.Sql(@"
                DELETE FROM contact_fields WHERE ""FieldType"" = 'Email';
            ");

            // Step 6: Migrate standalone phone from profiles to contact_fields
            // Note: FieldType stored as string, Visibility stored as string
            migrationBuilder.Sql(@"
                INSERT INTO contact_fields (""Id"", ""ProfileId"", ""FieldType"", ""CustomLabel"", ""Value"", ""Visibility"", ""DisplayOrder"", ""CreatedAt"", ""UpdatedAt"")
                SELECT gen_random_uuid(), p.""Id"", 'Phone', NULL,
                    COALESCE(p.""PhoneCountryCode"" || ' ', '') || p.""PhoneNumber"",
                    'AllActiveProfiles',
                    0, now(), now()
                FROM profiles p
                WHERE p.""PhoneNumber"" IS NOT NULL AND p.""PhoneNumber"" != '';
            ");

            // Step 7: Now create the unique index on verified emails
            migrationBuilder.CreateIndex(
                name: "IX_user_emails_Email",
                table: "user_emails",
                column: "Email",
                unique: true,
                filter: "\"IsVerified\" = true");

            // Step 8: Drop old columns
            migrationBuilder.DropIndex(
                name: "IX_users_PreferredEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PreferredEmail",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PreferredEmailVerificationSentAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PreferredEmailVerified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PhoneCountryCode",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "profiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_emails");

            migrationBuilder.AddColumn<string>(
                name: "PreferredEmail",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "PreferredEmailVerificationSentAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PreferredEmailVerified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneCountryCode",
                table: "profiles",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "profiles",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_PreferredEmail",
                table: "users",
                column: "PreferredEmail",
                unique: true,
                filter: "\"PreferredEmailVerified\" = true AND \"PreferredEmail\" IS NOT NULL");
        }
    }
}
