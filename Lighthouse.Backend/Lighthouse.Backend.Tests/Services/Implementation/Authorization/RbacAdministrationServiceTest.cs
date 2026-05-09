using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.Services.Implementation.Authorization
{
    [TestFixture]
    public class RbacAdministrationServiceTest
    {
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
            currentUserProfileService = new Mock<ICurrentUserProfileService>();
        }

        [Test]
        public async Task BootstrapCurrentUserAsSystemAdminAsync_NoSystemAdmin_AssignsCurrentUser()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|bootstrap-admin"));
            var currentUser = new UserProfile
            {
                Id = 42,
                Subject = "auth0|bootstrap-admin",
                SubjectClaimType = "sub",
                DisplayName = "Bootstrap Admin",
            };

            context.UserProfiles.Add(currentUser);
            await context.SaveChangesAsync();

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentUser);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.BootstrapCurrentUserAsSystemAdminAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.CountAsync(), Is.EqualTo(1));

                var grantedPermission = await context.UserPermissions.SingleAsync();
                Assert.That(grantedPermission.UserProfileId, Is.EqualTo(42));
                Assert.That(grantedPermission.Role, Is.EqualTo(UserRole.SystemAdmin));
                Assert.That(grantedPermission.ScopeType, Is.EqualTo(PermissionScopeType.System));
                Assert.That(grantedPermission.ScopeId, Is.Null);
            }
        }

        [Test]
        public async Task RevokeSystemAdminAsync_LastSystemAdmin_ReturnsFailureAndPreservesAssignment()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 7,
                Subject = "auth0|single-admin",
                SubjectClaimType = "sub",
            });

            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 7,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RevokeSystemAdminAsync(7, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.LastSystemAdmin));
                Assert.That(await context.UserPermissions.CountAsync(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetStatusAsync_ReturnsEnabledPremiumGateAndEmergencyAdminPresenceIndicator()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);
            context.UserProfiles.Add(new UserProfile
            {
                Id = 99,
                Subject = "auth0|existing-admin",
                SubjectClaimType = "sub",
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 99,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: ["auth0|break-glass"]);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.Enabled, Is.True);
                Assert.That(status.PremiumGateSatisfied, Is.True);
                Assert.That(status.HasSystemAdmin, Is.True);
                Assert.That(status.HasEmergencyAdminConfigured, Is.True);
                Assert.That(status.ReadyForEnablement, Is.True);
            }
        }

        [Test]
        public async Task CanManageRbacAsync_ConfiguredEmergencySubject_ReturnsTrueWithoutExplicitGrant()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|break-glass"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile
                {
                    Id = 22,
                    Subject = "auth0|break-glass",
                    SubjectClaimType = "sub",
                });

            var subject = CreateSubject(context, emergencySubjects: ["auth0|break-glass"]);

            var canManage = await subject.CanManageRbacAsync(principal, CancellationToken.None);

            Assert.That(canManage, Is.True);
        }

        [Test]
        public async Task IsRbacEnforcedAsync_WhenAuthorizationDisabled_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var isEnforced = await subject.IsRbacEnforcedAsync(CancellationToken.None);

            Assert.That(isEnforced, Is.False);
        }

        [Test]
        public async Task GetReadableTeamIdsAsync_WhenRbacEnabledAndUserHasScopedViewerRole_ReturnsOnlyReadableTeamIds()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 1,
                Subject = "auth0|system-admin",
                SubjectClaimType = "sub",
            });
            context.UserProfiles.Add(new UserProfile
            {
                Id = 2,
                Subject = "auth0|viewer",
                SubjectClaimType = "sub",
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 2,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 44,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var readableIds = await subject.GetReadableTeamIdsAsync(
                principal,
                [44, 45],
                CancellationToken.None);

            Assert.That(readableIds, Is.EqualTo([44]));
        }

        [Test]
        public async Task CanWritePortfolioAsync_WhenUserHasOnlyViewerRole_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 1,
                Subject = "auth0|system-admin",
                SubjectClaimType = "sub",
            });
            context.UserProfiles.Add(new UserProfile
            {
                Id = 2,
                Subject = "auth0|viewer",
                SubjectClaimType = "sub",
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 2,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 9,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWritePortfolioAsync(principal, 9, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task CanCreateTeamAsync_WhenRbacDisabled_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user-no-grants"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|user-no-grants", SubjectClaimType = "sub" });

            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var result = await subject.CanCreateTeamAsync(principal, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanCreateTeamAsync_WhenUserHasTeamAdminOnAnyTeam_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|team-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanCreateTeamAsync(principal, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanCreateTeamAsync_WhenUserHasNoTeamAdminAssignment_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanCreateTeamAsync(principal, CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanCreatePortfolioAsync_WhenRbacDisabled_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user-no-grants"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|user-no-grants", SubjectClaimType = "sub" });

            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var result = await subject.CanCreatePortfolioAsync(principal, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanCreatePortfolioAsync_WhenUserHasPortfolioAdminOnAnyPortfolio_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|portfolio-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanCreatePortfolioAsync(principal, CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanCreatePortfolioAsync_WhenUserHasNoPortfolioAdminAssignment_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanCreatePortfolioAsync(principal, CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_SystemAdmin_WhenRbacDisabled_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.SystemAdmin,
                null,
                CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_TeamRead_WhenUserHasViewerRole_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 44 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|viewer"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.TeamRead,
                44,
                CancellationToken.None);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_PortfolioWrite_WithoutScopeId_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.PortfolioWrite,
                null,
                CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_SystemAdmin_HasFullSystemCapabilities()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|system-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.True);
                Assert.That(summary.IsRbacEnabled, Is.True);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_TeamAdmin_CanCreateTeamButNotPortfolio()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|team-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.False);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.False);
                Assert.That(summary.IsRbacEnabled, Is.True);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_WhenRbacDisabled_AllCapabilitiesTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|any-user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|any-user", SubjectClaimType = "sub" });

            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.True);
                Assert.That(summary.IsRbacEnabled, Is.False);
            }
        }

        [Test]
        public async Task CanManageTeamMembershipAsync_WhenTeamAdminForDifferentTeam_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|team-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 44 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|team-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canManage = await subject.CanManageTeamMembershipAsync(principal, 45, CancellationToken.None);

            Assert.That(canManage, Is.False);
        }

        [Test]
        public async Task CanManagePortfolioMembershipAsync_WhenPortfolioAdminForScope_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|portfolio-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 9 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canManage = await subject.CanManagePortfolioMembershipAsync(principal, 9, CancellationToken.None);

            Assert.That(canManage, Is.True);
        }

        [Test]
        public async Task SetTeamMemberRoleAsync_WhenRoleInvalidForScope_ReturnsFailure()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 7, Subject = "auth0|target", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetTeamMemberRoleAsync(7, 12, UserRole.PortfolioAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.InvalidRoleForScope));
            }
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_WhenExistingViewerPresent_ReplacesWithPortfolioAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 7, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 7,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 12,
                Role = UserRole.Viewer,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(7, 12, UserRole.PortfolioAdmin, CancellationToken.None);

            var scopePermissions = await context.UserPermissions
                .Where(x => x.UserProfileId == 7 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 12)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(scopePermissions, Has.Count.EqualTo(1));
                Assert.That(scopePermissions[0].Role, Is.EqualTo(UserRole.PortfolioAdmin));
            }
        }

        [Test]
        public async Task RemoveTeamMemberAsync_RemovesScopedMembershipRows()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 8, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 8,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 21,
                Role = UserRole.Viewer,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemoveTeamMemberAsync(8, 21, CancellationToken.None);

            var remaining = await context.UserPermissions
                .Where(x => x.UserProfileId == 8 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 21)
                .CountAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(remaining, Is.EqualTo(0));
            }
        }

        private RbacAdministrationService CreateSubject(
            LighthouseAppContext context,
            IReadOnlyList<string> emergencySubjects,
            bool enabled = true)
        {
            var config = Options.Create(new AuthorizationConfiguration
            {
                Enabled = enabled,
                EmergencySystemAdminSubjects = emergencySubjects,
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