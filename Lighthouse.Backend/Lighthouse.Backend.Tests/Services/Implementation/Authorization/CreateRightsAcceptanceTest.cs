using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.Services.Implementation.Authorization
{
    [TestFixture]
    public class CreateRightsAcceptanceTest
    {
        private static readonly int[] AllSeededTeamIds = [10, 20, 30, 40];
        private static readonly int[] CreatorVisibleTeamIds = [10];

        private DbContextOptions<LighthouseAppContext> options;
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> appContextLogger;
        private Mock<ILogger<RbacAdministrationService>> serviceLogger;
        private Mock<ILicenseService> licenseService;
        private Mock<ICurrentUserProfileService> currentUserProfileService;

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoService = new Mock<ICryptoService>();
            appContextLogger = new Mock<ILogger<LighthouseAppContext>>();
            serviceLogger = new Mock<ILogger<RbacAdministrationService>>();
            licenseService = new Mock<ILicenseService>();
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);
            currentUserProfileService = new Mock<ICurrentUserProfileService>();
        }

        [Test]
        public async Task RbacGuard_CanCreateTeam_AdmitsTeamAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|team-admin", teamId: 10);
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreateTeam,
                scopeId: null,
                CancellationToken.None);

            Assert.That(
                canCreate,
                Is.True,
                "The CanCreateTeam guard must admit a non-System-Admin who holds at least one Team Admin role.");
        }

        [Test]
        public async Task RbacGuard_CanCreatePortfolio_AdmitsPortfolioAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedPortfolioAdmin(context, profileId: 2, subject: "auth0|portfolio-admin", portfolioId: 5);
            SeedTeam(context, teamId: 10, name: "Alpha");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            Assert.That(
                canCreate,
                Is.True,
                "The CanCreatePortfolio guard must admit a non-System-Admin who holds at least one Portfolio Admin role.");
        }

        [Test]
        public async Task RbacGuard_CanCreateTeam_AdmitsSystemAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            SetCurrentUser(principal, context, profileId: 1);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreateTeam,
                scopeId: null,
                CancellationToken.None);

            Assert.That(canCreate, Is.True, "System Admins must always be admitted by CanCreateTeam.");
        }

        [Test]
        public async Task RbacGuard_CanCreatePortfolio_AdmitsSystemAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeam(context, teamId: 10, name: "Alpha");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            SetCurrentUser(principal, context, profileId: 1);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            Assert.That(canCreate, Is.True, "System Admins must always be admitted by CanCreatePortfolio.");
        }

        [Test]
        public async Task RbacGuard_CanCreateTeam_RefusesViewer()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreateTeam,
                scopeId: null,
                CancellationToken.None);

            Assert.That(canCreate, Is.False, "Viewers must be refused by CanCreateTeam.");
        }

        [Test]
        public async Task RbacGuard_CanCreatePortfolio_RefusesViewer()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreate = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            Assert.That(canCreate, Is.False, "Viewers must be refused by CanCreatePortfolio.");
        }

        [Test]
        public async Task RbacGuard_CanCreateTeam_AdmitsGroupDerivedTeamAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeam(context, teamId: 10, name: "Alpha");
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|sam", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 10,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|sam"),
                new Claim("groups", "[\"team-admins\"]"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context, groupClaimName: "groups");

            var canCreateTeam = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreateTeam,
                scopeId: null,
                CancellationToken.None);

            var canCreatePortfolio = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    canCreateTeam,
                    Is.True,
                    "Team Admin rights derived from an SSO group mapping must enable team creation — group-based rights are behaviourally identical to direct user rights (rbac-enhancements WD-07).");
                Assert.That(
                    canCreatePortfolio,
                    Is.True,
                    "Under R2 unified rights, a group-derived Team Admin must also be able to create portfolios (provided a team exists in the system).");
            }
        }

        [Test]
        public async Task RbacGuard_CanCreatePortfolio_AdmitsGroupDerivedPortfolioAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeam(context, teamId: 10, name: "Alpha");
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|casey", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-admins",
                Role = UserRole.PortfolioAdmin,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 5,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|casey"),
                new Claim("groups", "[\"portfolio-admins\"]"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context, groupClaimName: "groups");

            var canCreatePortfolio = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            var canCreateTeam = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreateTeam,
                scopeId: null,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    canCreatePortfolio,
                    Is.True,
                    "Portfolio Admin rights derived from an SSO group mapping must enable portfolio creation — group-based rights are behaviourally identical to direct user rights (rbac-enhancements WD-07).");
                Assert.That(
                    canCreateTeam,
                    Is.True,
                    "Under R2 unified rights, a group-derived Portfolio Admin must also be able to create teams.");
            }
        }

        [Test]
        public async Task GrantCreatorTeamAdminAsync_AfterTeamCreation_RecordsCreatorAsTeamAdminOfNewTeam()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|jordan", teamId: 100);
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            const int newlyCreatedTeamId = 200;
            var result = await subject.GrantCreatorTeamAdminAsync(
                userProfileId: 2,
                teamId: newlyCreatedTeamId,
                CancellationToken.None);

            Assert.That(result.Succeeded, Is.True, "Granting the creator team-admin rights on the new team must succeed.");

            var grant = await context.UserPermissions.SingleOrDefaultAsync(p =>
                p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Team && p.ScopeId == newlyCreatedTeamId);

            Assert.That(grant, Is.Not.Null, "A UserPermission row must exist for the creator on the newly created team.");
            Assert.That(grant!.Role, Is.EqualTo(UserRole.TeamAdmin), "The creator must hold the TeamAdmin role on the newly created team.");
        }

        [Test]
        public async Task GrantCreatorPortfolioAdminAsync_AfterPortfolioCreation_RecordsCreatorAsPortfolioAdminOfNewPortfolio()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedPortfolioAdmin(context, profileId: 2, subject: "auth0|riley", portfolioId: 100);
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            const int newlyCreatedPortfolioId = 200;
            var result = await subject.GrantCreatorPortfolioAdminAsync(
                userProfileId: 2,
                portfolioId: newlyCreatedPortfolioId,
                CancellationToken.None);

            Assert.That(result.Succeeded, Is.True, "Granting the creator portfolio-admin rights on the new portfolio must succeed.");

            var grant = await context.UserPermissions.SingleOrDefaultAsync(p =>
                p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Portfolio && p.ScopeId == newlyCreatedPortfolioId);

            Assert.That(grant, Is.Not.Null, "A UserPermission row must exist for the creator on the newly created portfolio.");
            Assert.That(grant!.Role, Is.EqualTo(UserRole.PortfolioAdmin), "The creator must hold the PortfolioAdmin role on the newly created portfolio.");
        }

        [Test]
        public async Task GrantCreatorTeamAdminAsync_PreservesExistingTeamAdminScopes()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|jordan", teamId: 100);
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            await subject.GrantCreatorTeamAdminAsync(
                userProfileId: 2,
                teamId: 200,
                CancellationToken.None);

            var teamAdminScopes = await context.UserPermissions
                .Where(p => p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Team && p.Role == UserRole.TeamAdmin)
                .Select(p => p.ScopeId)
                .ToListAsync();

            Assert.That(
                teamAdminScopes,
                Is.EquivalentTo(new int?[] { 100, 200 }),
                "Granting creator-admin on a new team must not remove or alter the creator's existing TeamAdmin scopes.");
        }

        [Test]
        public async Task GrantCreatorPortfolioAdminAsync_PreservesExistingPortfolioAdminScopes()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedPortfolioAdmin(context, profileId: 2, subject: "auth0|riley", portfolioId: 100);
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            await subject.GrantCreatorPortfolioAdminAsync(
                userProfileId: 2,
                portfolioId: 200,
                CancellationToken.None);

            var portfolioAdminScopes = await context.UserPermissions
                .Where(p => p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Portfolio && p.Role == UserRole.PortfolioAdmin)
                .Select(p => p.ScopeId)
                .ToListAsync();

            Assert.That(
                portfolioAdminScopes,
                Is.EquivalentTo(new int?[] { 100, 200 }),
                "Granting creator-admin on a new portfolio must not remove or alter the creator's existing PortfolioAdmin scopes.");
        }

        [Test]
        public async Task GrantCreatorTeamAdminAsync_IsIdempotent()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            await subject.GrantCreatorTeamAdminAsync(userProfileId: 1, teamId: 200, CancellationToken.None);
            await subject.GrantCreatorTeamAdminAsync(userProfileId: 1, teamId: 200, CancellationToken.None);

            var teamAdminGrantCount = await context.UserPermissions.CountAsync(p =>
                p.UserProfileId == 1
                && p.ScopeType == PermissionScopeType.Team
                && p.ScopeId == 200
                && p.Role == UserRole.TeamAdmin);

            Assert.That(
                teamAdminGrantCount,
                Is.EqualTo(1),
                "Granting creator-admin twice for the same user/team must not create duplicate permission rows.");
        }

        [Test]
        public async Task GrantCreatorPortfolioAdminAsync_IsIdempotent()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            await subject.GrantCreatorPortfolioAdminAsync(userProfileId: 1, portfolioId: 200, CancellationToken.None);
            await subject.GrantCreatorPortfolioAdminAsync(userProfileId: 1, portfolioId: 200, CancellationToken.None);

            var portfolioAdminGrantCount = await context.UserPermissions.CountAsync(p =>
                p.UserProfileId == 1
                && p.ScopeType == PermissionScopeType.Portfolio
                && p.ScopeId == 200
                && p.Role == UserRole.PortfolioAdmin);

            Assert.That(
                portfolioAdminGrantCount,
                Is.EqualTo(1),
                "Granting creator-admin twice for the same user/portfolio must not create duplicate permission rows.");
        }

        [Test]
        public async Task WalkingSkeleton_TeamAdmin_CreatesPortfolio_WhenOtherTeamsExistButAreInvisible()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|team-admin", teamId: 10);
            SeedTeam(context, teamId: 10, name: "Alpha");
            SeedTeam(context, teamId: 20, name: "Beta");
            SeedTeam(context, teamId: 30, name: "Gamma");
            SeedTeam(context, teamId: 40, name: "Delta");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreatePortfolio = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            var readableTeamIds = await subject.GetReadableTeamIdsAsync(
                principal,
                AllSeededTeamIds,
                CancellationToken.None);

            const int newlyCreatedPortfolioId = 500;
            var grantResult = await subject.GrantCreatorPortfolioAdminAsync(
                userProfileId: 2,
                portfolioId: newlyCreatedPortfolioId,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canCreatePortfolio, Is.True, "A Team Admin must be admitted to create portfolios under R2 unified rights.");
                Assert.That(readableTeamIds, Is.EquivalentTo(CreatorVisibleTeamIds), "The creator only sees their own team — the existence gate must not be filtered by per-user visibility.");
                Assert.That(grantResult.Succeeded, Is.True, "Grant of PortfolioAdmin on the newly created portfolio must succeed for the creator.");
                Assert.That(
                    context.UserPermissions.Any(p => p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Portfolio && p.ScopeId == newlyCreatedPortfolioId && p.Role == UserRole.PortfolioAdmin),
                    Is.True,
                    "The creator's PortfolioAdmin grant for the new portfolio must be persisted.");
            }
        }

        [Test]
        public async Task AuthorizationSummary_SystemAdmin_CannotCreatePortfolio_WhenNoTeamsExist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            SetCurrentUser(principal, context, profileId: 1);
            var subject = CreateSubject(context);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True, "System Admins must always be able to create teams — the existence gate does not apply to team creation.");
                Assert.That(summary.CanCreatePortfolio, Is.False, "Even a System Admin must be refused portfolio creation when no teams exist anywhere in the system.");
            }
        }

        [Test]
        public async Task AuthorizationSummary_TeamAdmin_CanCreatePortfolio_WhenTeamExists()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|team-admin", teamId: 10);
            SeedTeam(context, teamId: 10, name: "Alpha");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.False);
                Assert.That(summary.CanCreateTeam, Is.True, "A Team Admin must be able to create teams.");
                Assert.That(summary.CanCreatePortfolio, Is.True, "Under R2 unified rights, a Team Admin must be able to create portfolios when at least one team exists.");
            }
        }

        [Test]
        public async Task AuthorizationSummary_PortfolioAdmin_CanCreateTeam()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedPortfolioAdmin(context, profileId: 2, subject: "auth0|portfolio-admin", portfolioId: 5);
            SeedTeam(context, teamId: 10, name: "Alpha");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.False);
                Assert.That(summary.CanCreateTeam, Is.True, "Under R2 unified rights, a Portfolio Admin must be able to create teams.");
                Assert.That(summary.CanCreatePortfolio, Is.True, "A Portfolio Admin must be able to create portfolios when at least one team exists.");
            }
        }

        [Test]
        public async Task AutoAdmin_TeamAdmin_WhoCreatesPortfolio_BecomesPortfolioAdminOfNewPortfolio()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|jordan", teamId: 100);
            SeedTeam(context, teamId: 100, name: "Alpha");
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            const int newlyCreatedPortfolioId = 700;
            var result = await subject.GrantCreatorPortfolioAdminAsync(
                userProfileId: 2,
                portfolioId: newlyCreatedPortfolioId,
                CancellationToken.None);

            var grant = await context.UserPermissions.SingleOrDefaultAsync(p =>
                p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Portfolio && p.ScopeId == newlyCreatedPortfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(grant, Is.Not.Null, "A Team Admin who creates a portfolio must become PortfolioAdmin of that new portfolio (cross-role auto-admin).");
                Assert.That(grant!.Role, Is.EqualTo(UserRole.PortfolioAdmin));
            }
        }

        [Test]
        public async Task AutoAdmin_PortfolioAdmin_WhoCreatesTeam_BecomesTeamAdminOfNewTeam()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedPortfolioAdmin(context, profileId: 2, subject: "auth0|riley", portfolioId: 100);
            await context.SaveChangesAsync();

            var subject = CreateSubject(context);

            const int newlyCreatedTeamId = 800;
            var result = await subject.GrantCreatorTeamAdminAsync(
                userProfileId: 2,
                teamId: newlyCreatedTeamId,
                CancellationToken.None);

            var grant = await context.UserPermissions.SingleOrDefaultAsync(p =>
                p.UserProfileId == 2 && p.ScopeType == PermissionScopeType.Team && p.ScopeId == newlyCreatedTeamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(grant, Is.Not.Null, "A Portfolio Admin who creates a team must become TeamAdmin of that new team (cross-role auto-admin).");
                Assert.That(grant!.Role, Is.EqualTo(UserRole.TeamAdmin));
            }
        }

        [Test]
        public async Task CanCreatePortfolio_RefusesEvenSystemAdmin_WhenNoTeamsExist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            SetCurrentUser(principal, context, profileId: 1);
            var subject = CreateSubject(context);

            var canCreatePortfolio = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            Assert.That(
                canCreatePortfolio,
                Is.False,
                "Portfolio creation must be refused — even for System Admin — when no team rows exist in the system. A portfolio without any team to roll up is structurally meaningless.");
        }

        [Test]
        public async Task CanCreatePortfolio_IsGlobal_NotVisibilityScoped()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            SeedSystemAdmin(context, profileId: 1, subject: "auth0|sysadmin");
            SeedTeamAdmin(context, profileId: 2, subject: "auth0|scoped-admin", teamId: 10);
            SeedTeam(context, teamId: 10, name: "Alpha");
            SeedTeam(context, teamId: 20, name: "Beta");
            SeedTeam(context, teamId: 30, name: "Gamma");
            SeedTeam(context, teamId: 40, name: "Delta");
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|scoped-admin"));
            SetCurrentUser(principal, context, profileId: 2);
            var subject = CreateSubject(context);

            var canCreatePortfolio = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.CanCreatePortfolio,
                scopeId: null,
                CancellationToken.None);

            var readableTeamIds = await subject.GetReadableTeamIdsAsync(
                principal,
                AllSeededTeamIds,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canCreatePortfolio, Is.True, "The portfolio existence gate must succeed because teams exist anywhere in the database.");
                Assert.That(readableTeamIds, Is.EquivalentTo(CreatorVisibleTeamIds), "The creator only has read access to their own team — the existence gate must NOT be visibility-filtered.");
            }
        }

        [Test]
        public async Task AuthorizationSummary_InRbacDisabledMode_BlocksPortfolioCreation_WhenNoTeamsExist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|any-user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|any-user", SubjectClaimType = "sub" });
            var subject = CreateSubject(context, enabled: false);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsRbacEnabled, Is.False);
                Assert.That(summary.CanCreateTeam, Is.True, "Team creation remains unconditionally permitted in RBAC-disabled mode.");
                Assert.That(
                    summary.CanCreatePortfolio,
                    Is.False,
                    "Even in RBAC-disabled mode, portfolio creation must be refused when no teams exist — the existence gate is unconditional.");
            }
        }

        [Test]
        public async Task AuthorizationSummary_InBootstrapNoAdminMode_BlocksPortfolioCreation_WhenNoTeamsExist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|first-user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|first-user", SubjectClaimType = "sub" });
            var subject = CreateSubject(context);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsRbacEnabled, Is.True);
                Assert.That(summary.IsSystemAdmin, Is.True, "Bootstrap mode grants any user system-admin-equivalent rights for setup.");
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(
                    summary.CanCreatePortfolio,
                    Is.False,
                    "Bootstrap mode must still refuse portfolio creation when no teams exist — the existence gate is unconditional.");
            }
        }

        private static void SeedTeam(LighthouseAppContext context, int teamId, string name)
        {
            context.Teams.Add(new Team { Id = teamId, Name = name });
        }

        private static void SeedSystemAdmin(LighthouseAppContext context, int profileId, string subject)
        {
            context.UserProfiles.Add(new UserProfile { Id = profileId, Subject = subject, SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = profileId, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
        }

        private static void SeedTeamAdmin(LighthouseAppContext context, int profileId, string subject, int teamId)
        {
            context.UserProfiles.Add(new UserProfile { Id = profileId, Subject = subject, SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = profileId, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = teamId });
        }

        private static void SeedPortfolioAdmin(LighthouseAppContext context, int profileId, string subject, int portfolioId)
        {
            context.UserProfiles.Add(new UserProfile { Id = profileId, Subject = subject, SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = profileId, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = portfolioId });
        }

        private void SetCurrentUser(ClaimsPrincipal principal, LighthouseAppContext context, int profileId)
        {
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == profileId));
        }

        private RbacAdministrationService CreateSubject(
            LighthouseAppContext context,
            bool enabled = true,
            string? groupClaimName = null)
        {
            var config = Options.Create(new AuthorizationConfiguration
            {
                Enabled = enabled,
                EmergencySystemAdminSubjects = [],
                GroupClaimName = groupClaimName,
            });

            return new RbacAdministrationService(
                context,
                config,
                licenseService.Object,
                currentUserProfileService.Object,
                serviceLogger.Object);
        }

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }
    }
}
