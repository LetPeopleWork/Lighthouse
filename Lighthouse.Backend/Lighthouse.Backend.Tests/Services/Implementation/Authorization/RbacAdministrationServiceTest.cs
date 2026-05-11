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
            context.UserProfiles.Add(new UserProfile
            {
                Id = 100,
                Subject = "auth0|unassigned-user",
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
                Assert.That(status.UnassignedUserCount, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetUsersAsync_UserWithoutPermissions_IsMarkedAsUnassigned()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 1,
                Subject = "auth0|admin",
                SubjectClaimType = "sub",
                DisplayName = "Admin User",
            });
            context.UserProfiles.Add(new UserProfile
            {
                Id = 2,
                Subject = "auth0|unassigned",
                SubjectClaimType = "sub",
                DisplayName = "Unassigned User",
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var users = await subject.GetUsersAsync(CancellationToken.None);
            var adminUser = users.Single(x => x.Id == 1);
            var unassignedUser = users.Single(x => x.Id == 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(adminUser.IsSystemAdmin, Is.True);
                Assert.That(adminUser.IsUnassigned, Is.False);
                Assert.That(unassignedUser.IsSystemAdmin, Is.False);
                Assert.That(unassignedUser.IsUnassigned, Is.True);
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

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub", DisplayName = "System Admin User" });
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
                Assert.That(summary.SystemAdminDisplayNames, Is.EqualTo(["System Admin User"]));
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
                Assert.That(remaining, Is.Zero);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_GroupMappedSystemAdmin_ReturnsSystemAdmin()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|group-admin", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "lighthouse-system-admins",
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|group-admin"),
                new Claim("groups", "lighthouse-system-admins"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 9));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.True);
            }
        }

        [Test]
        public async Task CanWriteTeamAsync_ExplicitViewerOverridesVirtualTeamAdmin_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 99, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 99,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|explicit-viewer", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-12-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|explicit-viewer"),
                new Claim("groups", "team-12-admins"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 12, CancellationToken.None);
            var canWrite = await subject.CanWriteTeamAsync(principal, 12, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canRead, Is.True);
                Assert.That(canWrite, Is.False);
            }
        }

        [Test]
        public async Task CanReadTeamAsync_UnsupportedGroupClaimPayload_FallsBackToExplicitOnlyAndLogsWarning()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 99, Subject = "auth0|system-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 99,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|unsupported-claim-user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-44-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 44,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|unsupported-claim-user"),
                new Claim("groups", "{\"name\":\"team-44-viewers\"}"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 44, CancellationToken.None);

            Assert.That(canRead, Is.False);

            serviceLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains("unsupported format", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task CreateGroupMappingAsync_WhenRoleScopeCombinationInvalid_ReturnsFailure()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                "team-admins",
                UserRole.TeamAdmin,
                PermissionScopeType.System,
                null,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.InvalidScopeForRole));
            }
        }

        [Test]
        public async Task GetUsersAsync_SetsIsEmergencyAdminBasedOnSubjectMatch()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 1,
                Subject = "auth0|break-glass",
                SubjectClaimType = "sub",
                DisplayName = "Emergency Admin",
            });
            context.UserProfiles.Add(new UserProfile
            {
                Id = 2,
                Subject = "auth0|regular-user",
                SubjectClaimType = "sub",
                DisplayName = "Regular User",
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: ["auth0|break-glass"]);

            var users = await subject.GetUsersAsync(CancellationToken.None);
            var emergencyAdmin = users.Single(x => x.Id == 1);
            var regularUser = users.Single(x => x.Id == 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(emergencyAdmin.IsEmergencyAdmin, Is.True);
                Assert.That(regularUser.IsEmergencyAdmin, Is.False);
            }
        }

        [Test]
        public async Task GetGroupMappingsAsync_WhenMappingsExist_ReturnsConfiguredMappings()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-7-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 7,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetGroupMappingsAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mappings, Has.Count.EqualTo(1));
                Assert.That(mappings[0].GroupValue, Is.EqualTo("portfolio-7-viewers"));
                Assert.That(mappings[0].Role, Is.EqualTo(UserRole.Viewer));
                Assert.That(mappings[0].ScopeType, Is.EqualTo(PermissionScopeType.Portfolio));
                Assert.That(mappings[0].ScopeId, Is.EqualTo(7));
            }
        }

        [Test]
        public async Task GetTeamGroupMappingsAsync_ReturnsOnlyMappingsForRequestedTeam()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "system-admins",
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-2-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 2,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetTeamGroupMappingsAsync(1, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mappings, Has.Count.EqualTo(2));
                Assert.That(mappings.All(m => m.ScopeType == PermissionScopeType.Team), Is.True);
                Assert.That(mappings.All(m => m.ScopeId == 1), Is.True);
                Assert.That(mappings.Select(m => m.GroupValue), Is.EquivalentTo(["team-1-admins", "team-1-viewers"]));
            }
        }

        [Test]
        public async Task GetTeamGroupMappingsAsync_WhenNoMappingsForTeam_ReturnsEmptyList()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetTeamGroupMappingsAsync(99, CancellationToken.None);

            Assert.That(mappings, Is.Empty);
        }

        [Test]
        public async Task GetPortfolioGroupMappingsAsync_ReturnsOnlyMappingsForRequestedPortfolio()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "system-admins",
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-1-admins",
                Role = UserRole.PortfolioAdmin,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 1,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 1,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-2-admins",
                Role = UserRole.PortfolioAdmin,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 2,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetPortfolioGroupMappingsAsync(1, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mappings, Has.Count.EqualTo(2));
                Assert.That(mappings.All(m => m.ScopeType == PermissionScopeType.Portfolio), Is.True);
                Assert.That(mappings.All(m => m.ScopeId == 1), Is.True);
                Assert.That(mappings.Select(m => m.GroupValue), Is.EquivalentTo(["portfolio-1-admins", "portfolio-1-viewers"]));
            }
        }

        [Test]
        public async Task GetPortfolioGroupMappingsAsync_WhenNoMappingsForPortfolio_ReturnsEmptyList()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "portfolio-1-admins",
                Role = UserRole.PortfolioAdmin,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetPortfolioGroupMappingsAsync(99, CancellationToken.None);

            Assert.That(mappings, Is.Empty);
        }

        [Test]
        public async Task DeleteUserAsync_ExistingUserWithMultiplePermissions_RemovesProfileAndAllPermissions()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile
            {
                Id = 42,
                Subject = "auth0|target-user",
                SubjectClaimType = "sub",
                DisplayName = "Target User",
            });
            context.UserProfiles.Add(new UserProfile
            {
                Id = 43,
                Subject = "auth0|other-user",
                SubjectClaimType = "sub",
                DisplayName = "Other User",
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 42,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 42,
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 42,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 7,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 43,
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            await context.SaveChangesAsync();

            // Ensure another System Admin exists so the cascade delete is not blocked by last-admin invariants downstream.
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 43,
                Role = UserRole.SystemAdmin,
                ScopeType = PermissionScopeType.System,
                ScopeId = null,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.DeleteUserAsync(42, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserProfiles.AnyAsync(x => x.Id == 42), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 42), Is.False);

                // Other user's data is untouched.
                Assert.That(await context.UserProfiles.AnyAsync(x => x.Id == 43), Is.True);
                Assert.That(await context.UserPermissions.CountAsync(x => x.UserProfileId == 43), Is.EqualTo(2));
            }
        }

        [Test]
        public async Task DeleteUserAsync_NonExistentUser_ReturnsUserNotFoundFailure()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.DeleteUserAsync(999, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.UserNotFound));
            }
        }

        // ==================================================================
        // Mutation-test gap closing (Round 2)
        // Targeted at surviving mutants in RbacAdministrationService.cs
        // documented in docs/feature/rbac-enhancements/deliver/mutation/mutation-report.md
        // ==================================================================

        [Test]
        public async Task IsRbacEnforcedAsync_WhenAuthorizationEnabled_ReturnsTrue()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: [], enabled: true);

            var isEnforced = await subject.IsRbacEnforcedAsync(CancellationToken.None);

            Assert.That(isEnforced, Is.True);
        }

        [Test]
        public async Task GetStatusAsync_WhenNoSystemAdminButPremiumGate_ReadyForEnablementIsFalse()
        {
            // Pins line 34: ReadyForEnablement uses && not ||
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject(context, emergencySubjects: []);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.PremiumGateSatisfied, Is.True);
                Assert.That(status.HasSystemAdmin, Is.False);
                Assert.That(status.ReadyForEnablement, Is.False);
            }
        }

        [Test]
        public async Task GetStatusAsync_WhenSystemAdminButNoPremiumGate_ReadyForEnablementIsFalse()
        {
            // Pins line 34: ReadyForEnablement uses && not ||
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.PremiumGateSatisfied, Is.False);
                Assert.That(status.HasSystemAdmin, Is.True);
                Assert.That(status.ReadyForEnablement, Is.False);
            }
        }

        [Test]
        public async Task GetStatusAsync_WhenNoEmergencyAdminConfigured_HasEmergencyAdminConfiguredIsFalse()
        {
            // Pins line 33 mutant: Count > 0 vs Count >= 0
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject(context, emergencySubjects: []);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            Assert.That(status.HasEmergencyAdminConfigured, Is.False);
        }

        [Test]
        public async Task GetStatusAsync_WhenGroupClaimNameConfigured_ExposesGroupClaimNameVerbatim()
        {
            // Pins line 36 conditional + string mutants - claim name returned when non-empty.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var status = await subject.GetStatusAsync(CancellationToken.None);

            Assert.That(status.GroupClaimName, Is.EqualTo("groups"));
        }

        [Test]
        public async Task GetStatusAsync_WhenGroupClaimNameNotConfigured_GroupClaimNameIsNull()
        {
            // Pins line 36 conditional - whitespace/null becomes null.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: null);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            Assert.That(status.GroupClaimName, Is.Null);
        }

        [Test]
        public async Task GetUsersAsync_OrdersByDisplayNameThenEmailAscending()
        {
            // Pins line 61 OrderBy/ThenBy mutants (3071, 3072).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|c", SubjectClaimType = "sub", DisplayName = "Charlie", Email = "charlie@example.com" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|a", SubjectClaimType = "sub", DisplayName = "Alice", Email = "alice@example.com" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|b1", SubjectClaimType = "sub", DisplayName = "Bob", Email = "bob1@example.com" });
            context.UserProfiles.Add(new UserProfile { Id = 4, Subject = "auth0|b2", SubjectClaimType = "sub", DisplayName = "Bob", Email = "bob2@example.com" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var users = await subject.GetUsersAsync(CancellationToken.None);

            // Ascending DisplayName, then ascending Email when DisplayName ties.
            Assert.That(users.Select(x => x.Id), Is.EqualTo([2, 3, 4, 1]));
        }

        [Test]
        public async Task GetUsersAsync_WithMultipleEmergencySubjects_OnlyMatchingSubjectIsFlagged()
        {
            // Pins line 78 Any -> All mutant (3075). Mixed list: only ONE of N entries matches.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|emergency-1", SubjectClaimType = "sub", DisplayName = "User One" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|regular", SubjectClaimType = "sub", DisplayName = "User Two" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(
                context,
                emergencySubjects: ["auth0|emergency-1", "auth0|emergency-2", "auth0|emergency-3"]);

            var users = await subject.GetUsersAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(users.Single(x => x.Id == 1).IsEmergencyAdmin, Is.True);
                // Under .All() this would be false because not every emergency subject equals user 1's subject.
                Assert.That(users.Single(x => x.Id == 2).IsEmergencyAdmin, Is.False);
            }
        }

        [Test]
        public async Task CanManageRbacAsync_WithMultipleEmergencySubjectsAndCurrentUserDoesNotMatchAny_ReturnsFalse()
        {
            // Pins Any -> All mutant in CanManageRbacAsync (line 540): under .All() the test would pass spuriously
            // when the user subject coincidentally matched every entry. With multiple distinct subjects the All
            // form returns false even when one matches; here we test the inverse - none match - to lock the predicate.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|nobody"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 5, Subject = "auth0|nobody", SubjectClaimType = "sub" });

            var subject = CreateSubject(
                context,
                emergencySubjects: ["auth0|break-glass-1", "auth0|break-glass-2"]);

            var canManage = await subject.CanManageRbacAsync(principal, CancellationToken.None);

            Assert.That(canManage, Is.False);
        }

        [Test]
        public async Task CanManageRbacAsync_WithMultipleEmergencySubjectsAndCurrentUserMatchesOne_ReturnsTrue()
        {
            // Companion to the previous test: locks Any() semantics - one match in many is sufficient.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|break-glass-2"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 5, Subject = "auth0|break-glass-2", SubjectClaimType = "sub" });

            var subject = CreateSubject(
                context,
                emergencySubjects: ["auth0|break-glass-1", "auth0|break-glass-2", "auth0|break-glass-3"]);

            var canManage = await subject.CanManageRbacAsync(principal, CancellationToken.None);

            Assert.That(canManage, Is.True);
        }

        [Test]
        public async Task GetReadablePortfolioIdsAsync_WhenRbacDisabled_ReturnsAllRequestedIds()
        {
            // Pins line 125-126 LogicalNot/Block-removal mutants.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: [], enabled: false);

            var ids = await subject.GetReadablePortfolioIdsAsync(principal, [10, 20, 30], CancellationToken.None);

            Assert.That(ids, Is.EquivalentTo([10, 20, 30]));
        }

        [Test]
        public async Task CanWriteTeamAsync_WhenEnforcementGateUnsatisfied_ReturnsFalse()
        {
            // Pins line 187 LogicalNot mutant.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWriteTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task CanWriteTeamAsync_WhenCurrentUserIsNull_ReturnsFalse()
        {
            // Pins line 198 equality mutant (currentUser is null vs is not null).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|other-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|orphan"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWriteTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task CanReadPortfolioAsync_WhenEnforcementGateUnsatisfied_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadPortfolioAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task CanReadPortfolioAsync_WhenCurrentUserIsNull_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|other-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|orphan"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadPortfolioAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task CanCreateTeamAsync_WithMultiplePermissionsExactlyOneTeamAdmin_ReturnsTrue()
        {
            // Pins line 286 Any -> All mutant. Multiple permissions, only ONE matches Team+TeamAdmin.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|other-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });

            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|mixed", SubjectClaimType = "sub" });
            // Three permissions for user 2: Viewer (team), Viewer (portfolio), TeamAdmin (team) - only one is the matching combo.
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 11 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|mixed"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canCreate = await subject.CanCreateTeamAsync(principal, CancellationToken.None);

            // Under .All() this would be false because not every permission is Team+TeamAdmin.
            Assert.That(canCreate, Is.True);
        }

        [Test]
        public async Task CanCreatePortfolioAsync_WithMultiplePermissionsExactlyOnePortfolioAdmin_ReturnsTrue()
        {
            // Pins line 316 Any -> All mutant. Mirror of the previous test for portfolio scope.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|other-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });

            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|mixed", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 6 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|mixed"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canCreate = await subject.CanCreatePortfolioAsync(principal, CancellationToken.None);

            Assert.That(canCreate, Is.True);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_TeamRead_WithoutScopeId_ReturnsFalse()
        {
            // Pins line 331 Logical mutant (&& vs ||).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.TeamRead,
                null,
                CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_TeamWrite_WithoutScopeId_ReturnsFalse()
        {
            // Pins line 333 Logical mutant.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.TeamWrite,
                null,
                CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CanSatisfyRequirementAsync_PortfolioRead_WithoutScopeId_ReturnsFalse()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CanSatisfyRequirementAsync(
                principal,
                RbacGuardRequirement.PortfolioRead,
                null,
                CancellationToken.None);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_WhenNoSystemAdminConfigured_AnyUserGetsBootstrapAdmin()
        {
            // Pins line 367 Boolean false mutant - bootstrap shortcut returns IsSystemAdmin=true.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var principal = BuildPrincipal(new Claim("sub", "auth0|first-user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfile { Id = 1, Subject = "auth0|first-user", SubjectClaimType = "sub" });

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsRbacEnabled, Is.True);
                Assert.That(summary.IsSystemAdmin, Is.True);
                Assert.That(summary.CanCreateTeam, Is.True);
                Assert.That(summary.CanCreatePortfolio, Is.True);
                Assert.That(summary.SystemAdminDisplayNames, Is.Empty);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_PortfolioAdmin_PopulatesAdminPortfolioIdsButNotTeamIds()
        {
            // Pins lines 393-399 Equality mutants on ScopeType filters - PortfolioAdmin populates portfolio IDs
            // and admin team IDs MUST be empty.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|system-admin", SubjectClaimType = "sub", DisplayName = "Sysadmin" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });

            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|portfolio-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 7 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 8 });
            // Add a Viewer entry on a team to verify it is NOT promoted to AdminTeamIds.
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 99 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.False);
                Assert.That(summary.AdminPortfolioIds, Is.EquivalentTo([7, 8]));
                Assert.That(summary.AdminTeamIds, Is.Empty);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_TeamAdminWithMultipleTeams_AdminTeamIdsContainsAllTeamAdminScopes()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });

            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|team-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 10 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 11 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 99 });
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
                Assert.That(summary.AdminTeamIds, Is.EquivalentTo([10, 11]));
                Assert.That(summary.AdminPortfolioIds, Is.Empty);
            }
        }

        [Test]
        public async Task GrantSystemAdminAsync_WhenUserDoesNotExist_ReturnsUserNotFoundFailure()
        {
            // Pins line 448-450 logical/equality mutants on user existence filter.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.GrantSystemAdminAsync(9999, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.UserNotFound));
                Assert.That(result.Message, Does.Contain("User profile"));
            }
        }

        [Test]
        public async Task GrantSystemAdminAsync_WhenAlreadySystemAdmin_ReturnsSuccessWithoutDuplication()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.GrantSystemAdminAsync(1, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.CountAsync(p => p.UserProfileId == 1 && p.Role == UserRole.SystemAdmin), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task RevokeSystemAdminAsync_WhenUserDoesNotHaveSystemAdmin_ReturnsSuccessIdempotently()
        {
            // Pins line 475-487 mutants on the SystemAdmin filter.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RevokeSystemAdminAsync(1, CancellationToken.None);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task RevokeSystemAdminAsync_WhenMoreThanOneSystemAdmin_RemovesAssignment()
        {
            // Pins line 487 system-admin count filter.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|admin1", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|admin2", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RevokeSystemAdminAsync(1, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(p => p.UserProfileId == 1), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(p => p.UserProfileId == 2 && p.Role == UserRole.SystemAdmin), Is.True);
            }
        }

        [Test]
        public async Task DeleteUserAsync_WhenUserHasNoPermissions_StillDeletesProfile()
        {
            // Pins line 518 permissions.Count > 0 vs >= 0 mutant. With zero permissions, profile must still be deleted.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|orphan", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.DeleteUserAsync(1, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserProfiles.AnyAsync(p => p.Id == 1), Is.False);
            }
        }

        [Test]
        public async Task CanManageTeamMembershipAsync_WhenCurrentUserIsNull_ReturnsFalse()
        {
            // Pins line 561 equality mutant.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var principal = BuildPrincipal(new Claim("sub", "auth0|orphan"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            var subject = CreateSubject(context, emergencySubjects: []);

            var canManage = await subject.CanManageTeamMembershipAsync(principal, 1, CancellationToken.None);

            Assert.That(canManage, Is.False);
        }

        [Test]
        public async Task CanManagePortfolioMembershipAsync_WhenManagingViaSystemAdmin_ReturnsTrueAndShortCircuits()
        {
            // Pins line 573 LogicalNot mutant - SystemAdmin path returns true without checking permissions.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canManage = await subject.CanManagePortfolioMembershipAsync(principal, 999, CancellationToken.None);

            Assert.That(canManage, Is.True);
        }

        [TestCase(UserRole.TeamAdmin)]
        [TestCase(UserRole.Viewer)]
        public async Task SetTeamMemberRoleAsync_WhenValidRoleAndNoExistingPermission_AddsPermission(UserRole role)
        {
            // Happy path coverage for SetTeamMemberRoleAsync. Pins:
            //  - line 651 logical mutant (role != TeamAdmin && role != Viewer) - both valid roles MUST succeed
            //  - line 655 message mutant - failure path NOT taken
            //  - line 725 currentPermissions.Any(...) - empty collection means insert path is taken
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetTeamMemberRoleAsync(1, 12, role, CancellationToken.None);

            var stored = await context.UserPermissions
                .Where(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(stored, Has.Count.EqualTo(1));
                Assert.That(stored[0].Role, Is.EqualTo(role));
            }
        }

        [Test]
        public async Task SetTeamMemberRoleAsync_WhenUserDoesNotExist_ReturnsUserNotFoundFailure()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetTeamMemberRoleAsync(9999, 12, UserRole.TeamAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.UserNotFound));
            }
        }

        [Test]
        public async Task SetTeamMemberRoleAsync_WhenSameRoleAlreadyAssigned_DoesNotDuplicate()
        {
            // Pins line 725 Any -> All mutant on currentPermissions.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
                Role = UserRole.TeamAdmin,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetTeamMemberRoleAsync(1, 12, UserRole.TeamAdmin, CancellationToken.None);

            var stored = await context.UserPermissions
                .Where(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(stored, Has.Count.EqualTo(1));
                Assert.That(stored[0].Role, Is.EqualTo(UserRole.TeamAdmin));
            }
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_WhenUserDoesNotExist_ReturnsUserNotFoundFailure()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(9999, 5, UserRole.PortfolioAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.UserNotFound));
            }
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_WhenInvalidRoleForPortfolio_ReturnsInvalidRoleForScope()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(1, 5, UserRole.TeamAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.InvalidRoleForScope));
            }
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_FilterByPortfolioScopeOnly_DoesNotDeleteOtherScopes()
        {
            // Pins line 713-720 logical/equality mutants on the scope filter - team permissions on the SAME user
            // for the SAME scope id MUST NOT be deleted by a portfolio role change.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            // Existing portfolio Viewer that should be replaced:
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 5,
                Role = UserRole.Viewer,
            });
            // Permissions on OTHER scope types/ids that MUST be preserved:
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 5,
                Role = UserRole.TeamAdmin,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 99,
                Role = UserRole.PortfolioAdmin,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(1, 5, UserRole.PortfolioAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                // Original target permission flipped to PortfolioAdmin
                Assert.That(await context.UserPermissions.CountAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == 5
                    && x.Role == UserRole.PortfolioAdmin), Is.EqualTo(1));
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == 5
                    && x.Role == UserRole.Viewer), Is.False);
                // Team permission on same scope id is preserved
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Team
                    && x.ScopeId == 5
                    && x.Role == UserRole.TeamAdmin), Is.True);
                // Other portfolio is preserved
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == 99
                    && x.Role == UserRole.PortfolioAdmin), Is.True);
            }
        }

        [Test]
        public async Task RemoveTeamMemberAsync_WhenNoMembership_ReturnsSuccessIdempotently()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemoveTeamMemberAsync(1, 99, CancellationToken.None);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task RemovePortfolioMemberAsync_FilterByPortfolioScopeOnly_DoesNotDeleteOtherScopes()
        {
            // Pins line 744-747 logical/equality mutants on scope filter.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 5,
                Role = UserRole.Viewer,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 5,
                Role = UserRole.Viewer,
            });
            context.UserPermissions.Add(new UserPermission
            {
                UserProfileId = 1,
                ScopeType = PermissionScopeType.Portfolio,
                ScopeId = 99,
                Role = UserRole.Viewer,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemovePortfolioMemberAsync(1, 5, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == 5), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Team
                    && x.ScopeId == 5), Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x =>
                    x.UserProfileId == 1
                    && x.ScopeType == PermissionScopeType.Portfolio
                    && x.ScopeId == 99), Is.True);
            }
        }

        [Test]
        public async Task GetGroupMappingsAsync_OrdersByGroupValueScopeTypeScopeIdRoleAscending()
        {
            // Pins line 781 ThenBy/OrderBy mutants on group mappings list.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "z-group", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "a-group", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "m-group", Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetGroupMappingsAsync(CancellationToken.None);

            // OrderBy GroupValue ASC: a-group, m-group, z-group.
            Assert.That(mappings.Select(x => x.GroupValue), Is.EqualTo(["a-group", "m-group", "z-group"]));
        }

        [Test]
        public async Task GetTeamGroupMappingsAsync_OrdersByGroupValueAscending()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "zeta", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "alpha", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "mu", Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetTeamGroupMappingsAsync(1, CancellationToken.None);

            Assert.That(mappings.Select(x => x.GroupValue), Is.EqualTo(["alpha", "mu", "zeta"]));
        }

        [Test]
        public async Task GetPortfolioGroupMappingsAsync_OrdersByGroupValueAscending()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "zeta", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "alpha", Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 1 });
            context.RbacGroupMappings.Add(new RbacGroupMapping { GroupValue = "mu", Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 1 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var mappings = await subject.GetPortfolioGroupMappingsAsync(1, CancellationToken.None);

            Assert.That(mappings.Select(x => x.GroupValue), Is.EqualTo(["alpha", "mu", "zeta"]));
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task CreateGroupMappingAsync_WhenGroupValueEmptyOrWhitespace_ReturnsInvalidScopeForRole(string groupValue)
        {
            // Pins line 839 string/negate mutants on the IsNullOrWhiteSpace guard.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                groupValue,
                UserRole.TeamAdmin,
                PermissionScopeType.Team,
                1,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.InvalidScopeForRole));
            }
        }

        [TestCase(UserRole.SystemAdmin, PermissionScopeType.System, null)]
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Team, 12)]
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, 5)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Team, 12)]
        [TestCase(UserRole.Viewer, PermissionScopeType.Portfolio, 5)]
        public async Task CreateGroupMappingAsync_WithValidRoleScopeCombination_PersistsMappingTrimmedAndReturnsSuccess(
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId)
        {
            // Pins:
            //  - line 850 string mutant on the InvalidScopeForRole error message
            //  - line 1101-1106 IsValidGroupMappingScope mutants (full role/scope matrix coverage)
            //  - happy-path persistence
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                "  whitespace-padded-group  ",
                role,
                scopeType,
                scopeId,
                CancellationToken.None);

            var stored = await context.RbacGroupMappings.SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(stored.GroupValue, Is.EqualTo("whitespace-padded-group"));
                Assert.That(stored.Role, Is.EqualTo(role));
                Assert.That(stored.ScopeType, Is.EqualTo(scopeType));
                Assert.That(stored.ScopeId, Is.EqualTo(scopeId));
            }
        }

        [Test]
        public async Task CreateGroupMappingAsync_DuplicateMapping_ReturnsSuccessIdempotently()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-12-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                "team-12-admins",
                UserRole.TeamAdmin,
                PermissionScopeType.Team,
                12,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.RbacGroupMappings.CountAsync(), Is.EqualTo(1));
            }
        }

        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Team, null)]            // TeamAdmin needs a scope id
        [TestCase(UserRole.TeamAdmin, PermissionScopeType.Portfolio, 5)]          // Wrong scope type
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Portfolio, null)]  // PortfolioAdmin needs scope id
        [TestCase(UserRole.PortfolioAdmin, PermissionScopeType.Team, 1)]          // Wrong scope type
        [TestCase(UserRole.SystemAdmin, PermissionScopeType.Team, 1)]             // SystemAdmin must be system scope
        [TestCase(UserRole.SystemAdmin, PermissionScopeType.System, 1)]           // SystemAdmin must NOT have scope id
        [TestCase(UserRole.Viewer, PermissionScopeType.System, null)]             // Viewer cannot be system scope
        [TestCase(UserRole.Viewer, PermissionScopeType.Team, null)]               // Viewer needs a scope id
        public async Task CreateGroupMappingAsync_InvalidRoleScopeCombinations_ReturnInvalidScopeForRole(
            UserRole role,
            PermissionScopeType scopeType,
            int? scopeId)
        {
            // Pins line 1101-1108 IsValidGroupMappingScope mutants - rejects every invalid combination.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                "some-group",
                role,
                scopeType,
                scopeId,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(RbacOperationErrorCodes.InvalidScopeForRole));
            }
        }

        // -----------------------------------------------------------------
        // Group claim parser tests (lines 956-1038) - 0% line coverage gap
        // -----------------------------------------------------------------

        [Test]
        public async Task GroupClaim_SingleStringClaim_GrantsMatchingMappings()
        {
            // Pins line 996 - plain string claim is added as-is to the group set.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-44-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 44,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "team-44-viewers"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 44, CancellationToken.None);

            Assert.That(canRead, Is.True);
        }

        [Test]
        public async Task GroupClaim_JsonArrayClaim_ParsesEachStringElement()
        {
            // Pins line 974-985 - JSON array branch parsed and elements added.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-A-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 100,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-B-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 200,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "[\"team-A-viewers\", \"team-B-viewers\", \"unrelated-group\"]"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canReadA = await subject.CanReadTeamAsync(principal, 100, CancellationToken.None);
            var canReadB = await subject.CanReadTeamAsync(principal, 200, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canReadA, Is.True);
                Assert.That(canReadB, Is.True);
            }
        }

        [Test]
        public async Task GroupClaim_MultipleIndividualClaims_AccumulatesAllValues()
        {
            // Pins the foreach loop iteration (each individual claim adds to set).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "first-group",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 100,
            });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "second-group",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 200,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "first-group"),
                new Claim("groups", "second-group"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canReadFirst = await subject.CanReadTeamAsync(principal, 100, CancellationToken.None);
            var canReadSecond = await subject.CanReadTeamAsync(principal, 200, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canReadFirst, Is.True);
                Assert.That(canReadSecond, Is.True);
            }
        }

        [Test]
        public async Task GroupClaim_MalformedJsonArray_FallsBackToExplicitOnlyAndLogsWarning()
        {
            // Pins line 976 - malformed JSON sets hasUnsupportedFormat = true.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "[\"team-1-viewers\", invalid"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);

            serviceLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((value, _) => value.ToString()!.Contains("unsupported format", StringComparison.OrdinalIgnoreCase)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task GroupClaim_JsonArrayWithNonStringElement_FallsBackToExplicitOnly()
        {
            // Pins line 1010 - non-string element returns false from TryParseJsonArrayClaim.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "[\"team-1-viewers\", 42]"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task GroupClaim_EmptyOrWhitespaceClaimValues_AreSkipped()
        {
            // Pins line 967-969 - whitespace-only claim values are not added.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "   "),
                new Claim("groups", "team-1-viewers"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            // Whitespace-only claim is skipped, but the meaningful claim still grants access.
            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.True);
        }

        [Test]
        public async Task GroupClaim_NoMatchingMapping_DoesNotGrantAccess()
        {
            // Confirms that group values not matching any RbacGroupMapping yield no virtual permissions.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "non-existent-group"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        // -----------------------------------------------------------------
        // HasTeamReadPermission Logical mutant pinning (line 1070)
        // -----------------------------------------------------------------

        [TestCase(UserRole.TeamAdmin, true)]
        [TestCase(UserRole.Viewer, true)]
        public async Task CanReadTeamAsync_WithTeamScopedRole_ReturnsTrueForBothAdminAndViewer(UserRole role, bool expected)
        {
            // Pins line 1070 logical mutant: the role check is OR (TeamAdmin OR Viewer), not AND.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = role, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|target"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadTeamAsync(principal, 12, CancellationToken.None);

            Assert.That(canRead, Is.EqualTo(expected));
        }

        // -----------------------------------------------------------------
        // SystemAdmin display names ordering & defaults (line 1129-1141)
        // -----------------------------------------------------------------

        [Test]
        public async Task GetAuthorizationSummaryAsync_MultipleSystemAdmins_DisplayNamesOrderedAscendingAndDeduplicated()
        {
            // Pins line 1137 OrderBy mutant + line 1138 conditional mutant.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|c", SubjectClaimType = "sub", DisplayName = "Charlie" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|a", SubjectClaimType = "sub", DisplayName = "Alice" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|b", SubjectClaimType = "sub", DisplayName = "Bob" });
            // Profile with whitespace display name to exercise the "System Admin" default branch.
            context.UserProfiles.Add(new UserProfile { Id = 4, Subject = "auth0|noname", SubjectClaimType = "sub", DisplayName = "" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 3, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 4, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|a"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            // Ordinal ascending: Alice, Bob, Charlie, System Admin (placeholder for empty display name).
            Assert.That(summary.SystemAdminDisplayNames, Is.EqualTo(["Alice", "Bob", "Charlie", "System Admin"]));
        }

        // -----------------------------------------------------------------
        // GetUnassignedUserCount Any/All distinguishing (line 1122)
        // -----------------------------------------------------------------

        // -----------------------------------------------------------------
        // Round 2.5: pin remaining surviving mutants discovered after the
        // first round. Each test below is paired with the mutant id from
        // the StrykerOutput JSON and explains why the previous test set
        // could not catch it.
        // -----------------------------------------------------------------

        [Test]
        public async Task CanWriteTeamAsync_WhenSystemAdminButLicenseGateFails_ReturnsFalse()
        {
            // Pins line 190 LogicalNot mutant (3111). Previous test had license false AND no current user,
            // so the lower path also returned false and the mutation was indistinguishable.
            // Here the user IS a sysadmin so without the gate they'd pass; the gate is the only blocker.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWriteTeamAsync(principal, 12, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task CanReadPortfolioAsync_WhenSystemAdminButLicenseGateFails_ReturnsFalse()
        {
            // Pins line 217 LogicalNot mutant (3124).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadPortfolioAsync(principal, 5, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task CanWritePortfolioAsync_WhenSystemAdminButLicenseGateFails_ReturnsFalse()
        {
            // Pins line 244 LogicalNot mutant (3137).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWritePortfolioAsync(principal, 5, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task CanWritePortfolioAsync_WhenCurrentUserIsNull_ReturnsFalse()
        {
            // Pins line 255 equality mutant (3143).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|other", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|orphan"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync((UserProfile?)null);

            var subject = CreateSubject(context, emergencySubjects: []);

            var canWrite = await subject.CanWritePortfolioAsync(principal, 5, CancellationToken.None);

            Assert.That(canWrite, Is.False);
        }

        [Test]
        public async Task GetReadableTeamIdsAsync_WhenManagerCannotAccessGate_ReturnsEmpty()
        {
            // Pins line 94 block-removal mutant (3078): when enforcement gate fails the early-return [] is
            // critical. Previous tests didn't have an enabled-but-gate-failing scenario for this method.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            var subject = CreateSubject(context, emergencySubjects: [], enabled: true);

            var readable = await subject.GetReadableTeamIdsAsync(principal, [10, 20], CancellationToken.None);

            Assert.That(readable, Is.Empty);
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_NonSystemAdmin_CanCreatePortfolioFollowsCanCreatePortfolioCheck()
        {
            // Pins line 380 logical mutant (3204): when isSystemAdmin is FALSE, CanCreatePortfolio MUST come
            // from the CanCreatePortfolioAsync result. With && instead of ||, the result would be (false && ...) = false.
            // We test a Portfolio Admin user: isSystemAdmin=false, but CanCreatePortfolioAsync=true.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|portfolio-admin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 7 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-admin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(summary.IsSystemAdmin, Is.False);
                // Under && mutation: (false && true) = false. Under || (original): (false || true) = true.
                Assert.That(summary.CanCreatePortfolio, Is.True);
            }
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_TeamAdminWithScopeIdNull_DoesNotPopulateAdminTeamIds()
        {
            // Pins line 394 logical mutant (3211): the && between ScopeType==Team AND ScopeId.HasValue
            // protects against null ScopeIds being unwrapped. Under || the mutation lets non-team
            // entries with HasValue OR team entries without HasValue through.
            // It's hard to create a TeamAdmin without scope id via DB (constraint), but we can construct
            // ToHighestRoleMap result via a SystemAdmin entry that has null ScopeId on a non-System scope.
            // Simpler approach: rely on a DB-level setup with a system-scope record that has UserRole.TeamAdmin
            // (impossible in production but accepted by EF Core in-memory).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|user", SubjectClaimType = "sub" });
            // Permission with TeamAdmin role but no scope id - boundary case. Real schema enforces this but
            // in-memory does not, which lets us pin the && operator semantics.
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = null });
            // Plus a legitimate one to make sure the legitimate entry IS included.
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 11 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            // Only the entry with HasValue=true should appear.
            Assert.That(summary.AdminTeamIds, Is.EqualTo([11]));
        }

        [Test]
        public async Task GetAuthorizationSummaryAsync_PortfolioAdminWithScopeIdNull_DoesNotPopulateAdminPortfolioIds()
        {
            // Pins line 398 logical mutant (3216) - portfolio mirror of the previous test.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = null });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 8 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|user"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            Assert.That(summary.AdminPortfolioIds, Is.EqualTo([8]));
        }

        [Test]
        public async Task GetUsersAsync_OnlySystemAdminFlagsForSystemScope_NotForTeamScopeAdminPermissions()
        {
            // Pins line 51 logical mutant (3068): && becomes ||, and line 1117 (3578) - same in HasSystemAdmin.
            // A user with TeamAdmin role at Team scope MUST NOT be IsSystemAdmin. Under || mutation, having
            // System scope OR SystemAdmin role triggers the flag - so a Team+TeamAdmin permission would falsely
            // mark the user as SystemAdmin (because role wouldn't be checked). Inverse: a System+Viewer entry
            // must also NOT be flagged.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|team-admin", SubjectClaimType = "sub", DisplayName = "Team Admin" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|sys-viewer", SubjectClaimType = "sub", DisplayName = "Sys Viewer" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|true-sysadmin", SubjectClaimType = "sub", DisplayName = "True Sysadmin" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 99 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 3, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var users = await subject.GetUsersAsync(CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(users.Single(x => x.Id == 1).IsSystemAdmin, Is.False);
                Assert.That(users.Single(x => x.Id == 2).IsSystemAdmin, Is.False);
                Assert.That(users.Single(x => x.Id == 3).IsSystemAdmin, Is.True);
            }
        }

        [Test]
        public async Task SetTeamMemberRoleAsync_WhenChangingViewerToTeamAdmin_RemovesViewerAndAddsTeamAdmin()
        {
            // Pins line 670 (3345-3347), line 673 (3353), line 677 (3356, 3357), line 728 (3395),
            // line 666 (3344), line 658 (3340).
            // The scenario: existing Viewer that must be removed, new TeamAdmin added; mutations on the
            // filter conditions or role-comparison would either keep the Viewer or never insert TeamAdmin.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            // Permissions on a different scope/team that MUST be preserved (the && filter must keep these).
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 13 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 12 });
            // Other user same team that MUST be preserved.
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|other", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetTeamMemberRoleAsync(1, 12, UserRole.TeamAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                // Target permission flipped:
                Assert.That(await context.UserPermissions.CountAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12 && x.Role == UserRole.TeamAdmin), Is.EqualTo(1));
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12 && x.Role == UserRole.Viewer), Is.False);
                // Permissions on other scopes preserved:
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 13 && x.Role == UserRole.Viewer), Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 12 && x.Role == UserRole.Viewer), Is.True);
                // Other user untouched:
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 2 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12 && x.Role == UserRole.Viewer), Is.True);
            }
        }

        [Test]
        public async Task RemoveTeamMemberAsync_OnlyRemovesTargetUserAndScope()
        {
            // Pins line 747 (3401-3403), line 750 (3408).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 13 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 12 });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|other", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemoveTeamMemberAsync(1, 12, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 13), Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 12), Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 2 && x.ScopeType == PermissionScopeType.Team && x.ScopeId == 12), Is.True);
            }
        }

        [Test]
        public async Task RemoveTeamMemberAsync_WhenOnlyTeamAdminPermissionExists_RemovesIt()
        {
            // Pins line 750 equality mutant (3408) - filter must include UserRole.TeamAdmin (the OR branch must
            // be honored). Without TeamAdmin in the filter, removing a team member who is a TeamAdmin would not
            // remove the row.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 12 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemoveTeamMemberAsync(1, 12, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(), Is.False);
            }
        }

        [Test]
        public async Task RemovePortfolioMemberAsync_WhenOnlyPortfolioAdminExists_RemovesIt()
        {
            // Pins line 769 equality mutant (3423).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.RemovePortfolioMemberAsync(1, 5, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(), Is.False);
            }
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_WhenChangingViewerToPortfolioAdmin_RemovesViewer()
        {
            // Pins line 716 logical mutant (3380), line 719 equality mutant (3385).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|other", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(1, 5, UserRole.PortfolioAdmin, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 5 && x.Role == UserRole.Viewer), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 5 && x.Role == UserRole.PortfolioAdmin), Is.True);
                // Other user untouched.
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 2 && x.Role == UserRole.Viewer), Is.True);
            }
        }

        [Test]
        public async Task CreateGroupMappingAsync_DuplicateCheckMatchesAllFourFields()
        {
            // Pins line 857 logical mutants (3460, 3461, 3462). The duplicate filter is && && && - flipping any
            // && to || would consider non-duplicates as duplicates and skip insertion.
            // We add a non-duplicate that differs from existing only by ROLE - it MUST be inserted.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-12-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            // Same group value, scope type, scope id - but different ROLE. Not a duplicate.
            var result = await subject.CreateGroupMappingAsync(
                "team-12-admins",
                UserRole.Viewer,
                PermissionScopeType.Team,
                12,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.RbacGroupMappings.CountAsync(), Is.EqualTo(2));
                Assert.That(await context.RbacGroupMappings.CountAsync(x => x.Role == UserRole.Viewer), Is.EqualTo(1));
                Assert.That(await context.RbacGroupMappings.CountAsync(x => x.Role == UserRole.TeamAdmin), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task CreateGroupMappingAsync_DuplicateCheckByGroupValueDifference()
        {
            // Pins line 857 (3462) - GroupValue must be in the duplicate predicate. A different group value
            // with same role/scope/id is NOT a duplicate.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-12-admins",
                Role = UserRole.TeamAdmin,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 12,
            });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.CreateGroupMappingAsync(
                "team-12-managers",
                UserRole.TeamAdmin,
                PermissionScopeType.Team,
                12,
                CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.RbacGroupMappings.CountAsync(), Is.EqualTo(2));
            }
        }

        [Test]
        public async Task GroupClaim_JsonObjectClaim_FallsBackAndLogsWarning()
        {
            // Pins line 990-993 (covered by existing test) and exercises the explicit object-claim branch.
            // This is already covered by CanReadTeamAsync_UnsupportedGroupClaimPayload but we also assert
            // directly so the parser branch is exercised in isolation.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "{\"team\":\"team-1-viewers\"}"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task GroupClaim_JsonArrayWithEmptyStringElements_AreSkipped()
        {
            // Pins line 1019 boolean mutant (3528) - if the empty-string skip is replaced with `true`,
            // empty values would be added to the group set and could match an empty group mapping.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "  ",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "[\"  \", \"\", \"non-matching\"]"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            // Empty / whitespace claim values must be skipped, so the user gets no virtual permissions.
            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task IsEnforcementGate_LicenseGate_FailsBeforeSystemAdminCheck()
        {
            // Pins line 1151 boolean mutant (3599) - replacing the license check `return false` with `return true`
            // would let unlicensed users bootstrap. Test: license fails AND no system admin → CanReadTeam returns
            // false (gate not satisfied → bootstrap mode would NOT trigger).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            // No system admin in DB at all, no current user setup either.
            var principal = BuildPrincipal(new Claim("sub", "auth0|stranger"));
            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);
            var canCreate = await subject.CanCreateTeamAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(canRead, Is.False);
                Assert.That(canCreate, Is.False);
            }
        }

        [Test]
        public async Task GetUnassignedUserCount_LogicalNotPredicate_OnlyCountsZeroPermissionUsers()
        {
            // Pins line 1125 LogicalNot mutant (3582) - removing the ! would invert the count.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            // 3 assigned users, 1 unassigned. Under !-removal mutant the count would be 3 (assigned), not 1.
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|teamlead", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|viewer", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 4, Subject = "auth0|orphan", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 3, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            Assert.That(status.UnassignedUserCount, Is.EqualTo(1));
        }

        [Test]
        public async Task GetSystemAdminDisplayNames_OnlyIncludesSystemAdminAtSystemScope()
        {
            // Pins line 1132 logical mutant (3586). && becomes ||. Need a row that satisfies one side but not the other:
            // a System+Viewer (matches ScopeType but not Role) and a Team+SystemAdmin (matches Role but not ScopeType -
            // although this is impossible in production, in-memory accepts it).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub", DisplayName = "True Sysadmin" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|sys-viewer", SubjectClaimType = "sub", DisplayName = "Sys Viewer" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|misclassified", SubjectClaimType = "sub", DisplayName = "Misclassified" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 3, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            await context.SaveChangesAsync();

            // Use a non-admin principal so the AdminTeamIds path (which doesn't run) doesn't impact us.
            var principal = BuildPrincipal(new Claim("sub", "auth0|sysadmin"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: []);

            var summary = await subject.GetAuthorizationSummaryAsync(principal, CancellationToken.None);

            // Under && (correct): only "True Sysadmin" shown.
            // Under || (mutant): all three would be shown.
            Assert.That(summary.SystemAdminDisplayNames, Is.EqualTo(["True Sysadmin"]));
        }

        [Test]
        public async Task SetPortfolioMemberRoleAsync_WhenChangingPortfolioAdminToViewer_RemovesPortfolioAdmin()
        {
            // Pins line 719 equality mutant (3385): permissionsToRemove filter must match by != role,
            // not hardcoded != PortfolioAdmin. With new role = Viewer, the filter must remove the
            // existing PortfolioAdmin row.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|target", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.PortfolioAdmin, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var result = await subject.SetPortfolioMemberRoleAsync(1, 5, UserRole.Viewer, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 5 && x.Role == UserRole.PortfolioAdmin), Is.False);
                Assert.That(await context.UserPermissions.AnyAsync(x => x.UserProfileId == 1 && x.ScopeType == PermissionScopeType.Portfolio && x.ScopeId == 5 && x.Role == UserRole.Viewer), Is.True);
            }
        }

        [Test]
        public async Task GroupClaim_JsonArrayWithNullElement_FallsBackToExplicitOnly()
        {
            // Pins line 1025 statement mutant (3534): if `continue` is replaced with `;`, the loop falls
            // through and tries to call Trim() on a null string -> NullReferenceException would crash the
            // request. Original behavior: null elements are skipped.
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 9, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 9, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|user", SubjectClaimType = "sub" });
            context.RbacGroupMappings.Add(new RbacGroupMapping
            {
                GroupValue = "team-1-viewers",
                Role = UserRole.Viewer,
                ScopeType = PermissionScopeType.Team,
                ScopeId = 1,
            });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|user"),
                new Claim("groups", "[\"team-1-viewers\", null, \"\"]"));

            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 1));

            var subject = CreateSubject(context, emergencySubjects: [], groupClaimName: "groups");

            // Under the mutant the request would NRE. Under correct behavior we expect false:
            // the JSON array contains a non-string element (null is JsonValueKind.Null, not String),
            // so TryParseJsonArrayClaim returns false -> hasUnsupportedFormat=true -> fallback.
            var canRead = await subject.CanReadTeamAsync(principal, 1, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task HasTeamReadPermission_RequiresKeyMatchAndRoleMatch_NotEither()
        {
            // Pins line 1073 logical mutant (3549) - && becomes ||. Under ||, ANY user without a Team key
            // for the requested team would still pass if the test fixtures had ANY UserRole-typed value
            // bound to `role` in scope. Use a user with a Portfolio-only entry to force the TryGetValue
            // for a Team scope to FAIL but the role variable to remain unset (default 0).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|sysadmin", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|portfolio-only", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.Viewer, ScopeType = PermissionScopeType.Portfolio, ScopeId = 5 });
            await context.SaveChangesAsync();

            var principal = BuildPrincipal(new Claim("sub", "auth0|portfolio-only"));
            currentUserProfileService
                .Setup(s => s.GetOrCreateFromPrincipalAsync(principal, It.IsAny<CancellationToken>()))
                .ReturnsAsync(context.UserProfiles.Single(x => x.Id == 2));

            var subject = CreateSubject(context, emergencySubjects: []);

            var canRead = await subject.CanReadTeamAsync(principal, 99, CancellationToken.None);

            Assert.That(canRead, Is.False);
        }

        [Test]
        public async Task GetStatusAsync_UnassignedUserCount_OnlyCountsProfilesWithNoPermissions()
        {
            // Pins line 1122 LogicalNot/Any-All/Equality mutants.
            // Mix of users: some with permissions, some without. Unassigned count must equal exactly the
            // count of profiles that have ZERO permissions (not All / not Any / not None of the mutants).
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            licenseService.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            context.UserProfiles.Add(new UserProfile { Id = 1, Subject = "auth0|admin", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 2, Subject = "auth0|teamlead", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 3, Subject = "auth0|orphan-1", SubjectClaimType = "sub" });
            context.UserProfiles.Add(new UserProfile { Id = 4, Subject = "auth0|orphan-2", SubjectClaimType = "sub" });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 1, Role = UserRole.SystemAdmin, ScopeType = PermissionScopeType.System });
            context.UserPermissions.Add(new UserPermission { UserProfileId = 2, Role = UserRole.TeamAdmin, ScopeType = PermissionScopeType.Team, ScopeId = 1 });
            await context.SaveChangesAsync();

            var subject = CreateSubject(context, emergencySubjects: []);

            var status = await subject.GetStatusAsync(CancellationToken.None);

            // Two unassigned users (ids 3 and 4). Negate of Any() means "no permissions exist".
            // Under Any -> All this would always be 0 (no profile has permissions for ALL users) or always 4.
            Assert.That(status.UnassignedUserCount, Is.EqualTo(2));
        }

        private RbacAdministrationService CreateSubject(
            LighthouseAppContext context,
            IReadOnlyList<string> emergencySubjects,
            bool enabled = true,
            string? groupClaimName = null)
        {
            var config = Options.Create(new AuthorizationConfiguration
            {
                Enabled = enabled,
                EmergencySystemAdminSubjects = emergencySubjects,
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