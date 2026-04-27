using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropOpsCopilot.Domain.Entities;
using PropOpsCopilot.Domain.Enums;
using PropOpsCopilot.Infrastructure.Identity;

namespace PropOpsCopilot.Infrastructure.Persistence;

public sealed class PropOpsDataSeeder(
    PropOpsDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await EnsureOperationalSchemaAsync(cancellationToken);
        await EnsureIdentitySchemaAsync(cancellationToken);
        await SeedContactDirectoryAsync(cancellationToken);
        await SeedPortalUsersAsync();

        if (await dbContext.MaintenanceRequests.AnyAsync(cancellationToken))
        {
            return;
        }

        var seedRequests = new[]
        {
            MaintenanceRequest.Create(
                "Ava Thompson",
                "ava.thompson@example.com",
                "0412 200 100",
                "Harbour View Residences",
                "12B",
                "Kitchen sink is leaking heavily under the cabinet and the flooring is getting wet.",
                MaintenanceRequestCategory.Plumbing,
                MaintenanceRequestPriority.High,
                IntakeChannel.Portal),
            MaintenanceRequest.Create(
                "Leo Chen",
                "leo.chen@example.com",
                "0412 200 101",
                "Northside Apartments",
                "8A",
                "Main bedroom air conditioning is not cooling and the apartment is very warm.",
                MaintenanceRequestCategory.HVAC,
                MaintenanceRequestPriority.Normal,
                IntakeChannel.Email),
            MaintenanceRequest.Create(
                "Mia Patel",
                "mia.patel@example.com",
                "0412 200 102",
                "Elm Street Townhomes",
                "3",
                "Front door lock is jammed and the property cannot be secured properly.",
                MaintenanceRequestCategory.Security,
                MaintenanceRequestPriority.Emergency,
                IntakeChannel.SmsChat),
            MaintenanceRequest.Create(
                "Noah Williams",
                "noah.williams@example.com",
                "0412 200 103",
                "Cityscape Lofts",
                "19D",
                "Dishwasher has stopped mid-cycle and is leaving water in the base tray.",
                MaintenanceRequestCategory.Appliances,
                MaintenanceRequestPriority.Low,
                IntakeChannel.PhoneNote)
        };

        seedRequests[0].TransitionTo(MaintenanceRequestStatus.InReview);
        seedRequests[1].TransitionTo(MaintenanceRequestStatus.Scheduled);
        seedRequests[2].TransitionTo(MaintenanceRequestStatus.InProgress);

        await dbContext.MaintenanceRequests.AddRangeAsync(seedRequests, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOperationalSchemaAsync(CancellationToken cancellationToken)
    {
        const string operationalSchemaSql = """
            CREATE TABLE IF NOT EXISTS contact_directory_entries (
                "Id" uuid NOT NULL,
                "FullName" character varying(120) NOT NULL,
                "EmailAddress" character varying(256) NOT NULL DEFAULT '',
                "PhoneNumber" character varying(32) NOT NULL DEFAULT '',
                "PropertyName" character varying(160) NOT NULL,
                "UnitNumber" character varying(40) NOT NULL DEFAULT '',
                "TenantName" character varying(120) NOT NULL,
                CONSTRAINT "PK_contact_directory_entries" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS intake_submissions (
                "Id" uuid NOT NULL,
                "SourceReference" character varying(80) NOT NULL,
                "Channel" character varying(40) NOT NULL,
                "ReceivedAtUtc" timestamp with time zone NOT NULL,
                "SubmitterName" character varying(120) NOT NULL,
                "TenantName" character varying(120) NOT NULL,
                "EmailAddress" character varying(256) NOT NULL DEFAULT '',
                "PhoneNumber" character varying(32) NOT NULL DEFAULT '',
                "PropertyName" character varying(160) NOT NULL,
                "UnitNumber" character varying(40) NOT NULL DEFAULT '',
                "Subject" character varying(240) NOT NULL DEFAULT '',
                "RawContent" character varying(6000) NOT NULL,
                "NormalizedContent" character varying(4000) NOT NULL,
                "Category" character varying(40) NOT NULL,
                "Priority" character varying(40) NOT NULL,
                "IsAfterHours" boolean NOT NULL,
                "MetadataMatched" boolean NOT NULL,
                "MaintenanceRequestId" uuid NOT NULL,
                CONSTRAINT "PK_intake_submissions" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_intake_submissions_maintenance_requests_MaintenanceRequestId"
                    FOREIGN KEY ("MaintenanceRequestId") REFERENCES maintenance_requests ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS maintenance_triage_reviews (
                "Id" uuid NOT NULL,
                "MaintenanceRequestId" uuid NOT NULL,
                "AiCategory" character varying(40) NOT NULL,
                "AiPriority" character varying(40) NOT NULL,
                "AiVendorType" character varying(160) NOT NULL,
                "AiDispatchDecision" character varying(1000) NOT NULL,
                "AiInternalSummary" character varying(1200) NOT NULL,
                "AiTenantResponseDraft" character varying(1200) NOT NULL,
                "FinalCategory" character varying(40) NOT NULL,
                "FinalPriority" character varying(40) NOT NULL,
                "FinalVendorType" character varying(160) NOT NULL,
                "FinalDispatchDecision" character varying(1000) NOT NULL,
                "FinalInternalSummary" character varying(1200) NOT NULL,
                "FinalTenantResponseDraft" character varying(1200) NOT NULL,
                "GuardrailRequiresHumanReview" boolean NOT NULL,
                "GuardrailSummary" character varying(2000) NOT NULL,
                "Status" character varying(40) NOT NULL,
                "ReviewedBy" character varying(256) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "ReviewedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_maintenance_triage_reviews" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_maintenance_triage_reviews_maintenance_requests_MaintenanceRequestId"
                    FOREIGN KEY ("MaintenanceRequestId") REFERENCES maintenance_requests ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS maintenance_operational_actions (
                "Id" uuid NOT NULL,
                "MaintenanceRequestId" uuid NOT NULL,
                "ActionType" character varying(60) NOT NULL,
                "Detail" character varying(2000) NOT NULL,
                "ExternalReference" character varying(80) NOT NULL,
                "CreatedBy" character varying(256) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_maintenance_operational_actions" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_maintenance_operational_actions_maintenance_requests_MaintenanceRequestId"
                    FOREIGN KEY ("MaintenanceRequestId") REFERENCES maintenance_requests ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS maintenance_resolution_feedback (
                "Id" uuid NOT NULL,
                "MaintenanceRequestId" uuid NOT NULL,
                "MaintenanceTriageReviewId" uuid NULL,
                "FinalResolution" character varying(2000) NOT NULL,
                "CorrectedCategory" character varying(40) NOT NULL,
                "CorrectedPriority" character varying(40) NOT NULL,
                "FinalTenantResponse" character varying(1200) NOT NULL,
                "DispatchOutcome" character varying(60) NOT NULL,
                "ResolutionNotes" character varying(2000) NOT NULL,
                "ExcludeFromTraining" boolean NOT NULL,
                "ExclusionReason" character varying(1000) NOT NULL,
                "ResolvedBy" character varying(256) NOT NULL,
                "ResolvedAtUtc" timestamp with time zone NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_maintenance_resolution_feedback" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_maintenance_resolution_feedback_maintenance_requests_MaintenanceRequestId"
                    FOREIGN KEY ("MaintenanceRequestId") REFERENCES maintenance_requests ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_maintenance_resolution_feedback_maintenance_triage_reviews_MaintenanceTriageReviewId"
                    FOREIGN KEY ("MaintenanceTriageReviewId") REFERENCES maintenance_triage_reviews ("Id") ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS fine_tuning_example_candidates (
                "Id" uuid NOT NULL,
                "MaintenanceRequestId" uuid NOT NULL,
                "MaintenanceResolutionFeedbackId" uuid NOT NULL,
                "Status" character varying(40) NOT NULL,
                "InputSnapshotJson" jsonb NOT NULL,
                "OutputSnapshotJson" jsonb NOT NULL,
                "MetadataSnapshotJson" jsonb NOT NULL,
                "ExclusionReason" character varying(1000) NOT NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                CONSTRAINT "PK_fine_tuning_example_candidates" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_fine_tuning_example_candidates_maintenance_resolution_feedback_MaintenanceResolutionFeedbackId"
                    FOREIGN KEY ("MaintenanceResolutionFeedbackId") REFERENCES maintenance_resolution_feedback ("Id") ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS "IX_contact_directory_entries_EmailAddress"
                ON contact_directory_entries ("EmailAddress");
            CREATE INDEX IF NOT EXISTS "IX_contact_directory_entries_PhoneNumber"
                ON contact_directory_entries ("PhoneNumber");
            CREATE INDEX IF NOT EXISTS "IX_intake_submissions_MaintenanceRequestId"
                ON intake_submissions ("MaintenanceRequestId");
            CREATE INDEX IF NOT EXISTS "IX_intake_submissions_ReceivedAtUtc"
                ON intake_submissions ("ReceivedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_intake_submissions_SourceReference"
                ON intake_submissions ("SourceReference");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_triage_reviews_MaintenanceRequestId"
                ON maintenance_triage_reviews ("MaintenanceRequestId");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_triage_reviews_ReviewedAtUtc"
                ON maintenance_triage_reviews ("ReviewedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_operational_actions_MaintenanceRequestId"
                ON maintenance_operational_actions ("MaintenanceRequestId");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_operational_actions_CreatedAtUtc"
                ON maintenance_operational_actions ("CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_resolution_feedback_MaintenanceRequestId"
                ON maintenance_resolution_feedback ("MaintenanceRequestId");
            CREATE INDEX IF NOT EXISTS "IX_maintenance_resolution_feedback_ResolvedAtUtc"
                ON maintenance_resolution_feedback ("ResolvedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_fine_tuning_example_candidates_MaintenanceRequestId"
                ON fine_tuning_example_candidates ("MaintenanceRequestId");
            CREATE INDEX IF NOT EXISTS "IX_fine_tuning_example_candidates_CreatedAtUtc"
                ON fine_tuning_example_candidates ("CreatedAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_fine_tuning_example_candidates_Status"
                ON fine_tuning_example_candidates ("Status");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(operationalSchemaSql, cancellationToken);
    }

    private async Task EnsureIdentitySchemaAsync(CancellationToken cancellationToken)
    {
        const string identitySchemaSql = """
            CREATE TABLE IF NOT EXISTS "AspNetRoles" (
                "Id" text NOT NULL,
                "Name" character varying(256),
                "NormalizedName" character varying(256),
                "ConcurrencyStamp" text,
                CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "AspNetUsers" (
                "Id" text NOT NULL,
                "FullName" text NOT NULL,
                "UserName" character varying(256),
                "NormalizedUserName" character varying(256),
                "Email" character varying(256),
                "NormalizedEmail" character varying(256),
                "EmailConfirmed" boolean NOT NULL,
                "PasswordHash" text,
                "SecurityStamp" text,
                "ConcurrencyStamp" text,
                "PhoneNumber" text,
                "PhoneNumberConfirmed" boolean NOT NULL,
                "TwoFactorEnabled" boolean NOT NULL,
                "LockoutEnd" timestamp with time zone,
                "LockoutEnabled" boolean NOT NULL,
                "AccessFailedCount" integer NOT NULL,
                CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
            );

            CREATE TABLE IF NOT EXISTS "AspNetRoleClaims" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY,
                "RoleId" text NOT NULL,
                "ClaimType" text,
                "ClaimValue" text,
                CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "AspNetUserClaims" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY,
                "UserId" text NOT NULL,
                "ClaimType" text,
                "ClaimValue" text,
                CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
                CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "AspNetUserLogins" (
                "LoginProvider" text NOT NULL,
                "ProviderKey" text NOT NULL,
                "ProviderDisplayName" text,
                "UserId" text NOT NULL,
                CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
                CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "AspNetUserRoles" (
                "UserId" text NOT NULL,
                "RoleId" text NOT NULL,
                CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
                CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS "AspNetUserTokens" (
                "UserId" text NOT NULL,
                "LoginProvider" text NOT NULL,
                "Name" text NOT NULL,
                "Value" text,
                CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
                CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");
            CREATE INDEX IF NOT EXISTS "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");
            CREATE INDEX IF NOT EXISTS "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");
            CREATE UNIQUE INDEX IF NOT EXISTS "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");
            CREATE INDEX IF NOT EXISTS "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");
            CREATE INDEX IF NOT EXISTS "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");
            """;

        await dbContext.Database.ExecuteSqlRawAsync(identitySchemaSql, cancellationToken);
    }

    private async Task SeedPortalUsersAsync()
    {
        foreach (var role in PortalRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureSucceeded(roleResult, $"create role '{role}'");
            }
        }

        await EnsureUserAsync(
            email: "manager@propops.local",
            fullName: "Jordan Blake",
            role: PortalRoles.PropertyManager,
            password: "PropOps!Manager1");

        await EnsureUserAsync(
            email: "dispatcher@propops.local",
            fullName: "Casey Morgan",
            role: PortalRoles.Dispatcher,
            password: "PropOps!Dispatch1");

        await EnsureUserAsync(
            email: "tenant@propops.local",
            fullName: "Ava Thompson",
            role: PortalRoles.Tenant,
            password: "PropOps!Tenant1");

        await EnsureUserAsync(
            email: "owner@propops.local",
            fullName: "Harper Ellis",
            role: PortalRoles.PropertyOwner,
            password: "PropOps!Owner1");

        await EnsureUserAsync(
            email: "vendor@propops.local",
            fullName: "Riverstone Plumbing",
            role: PortalRoles.Vendor,
            password: "PropOps!Vendor1");
    }

    private async Task EnsureUserAsync(string email, string fullName, string role, string password)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, $"create user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            EnsureSucceeded(roleResult, $"assign role '{role}' to '{email}'");
        }
    }

    private async Task SeedContactDirectoryAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ContactDirectoryEntries.AnyAsync(cancellationToken))
        {
            return;
        }

        var contacts = new[]
        {
            ContactDirectoryEntry.Create(
                "Ava Thompson",
                "ava.thompson@example.com",
                "0412 200 100",
                "Harbour View Residences",
                "12B",
                "Ava Thompson"),
            ContactDirectoryEntry.Create(
                "Leo Chen",
                "leo.chen@example.com",
                "0412 200 101",
                "Northside Apartments",
                "8A",
                "Leo Chen"),
            ContactDirectoryEntry.Create(
                "Mia Patel",
                "mia.patel@example.com",
                "0412 200 102",
                "Elm Street Townhomes",
                "3",
                "Mia Patel"),
            ContactDirectoryEntry.Create(
                "Noah Williams",
                "noah.williams@example.com",
                "0412 200 103",
                "Cityscape Lofts",
                "19D",
                "Noah Williams"),
            ContactDirectoryEntry.Create(
                "Harper Ellis",
                "owner@propops.local",
                "0412 200 104",
                "Harbour View Residences",
                string.Empty,
                "Harbour View Residences Owner")
        };

        await dbContext.ContactDirectoryEntries.AddRangeAsync(contacts, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {action}: {errors}");
    }
}
