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

        private RbacAdministrationService CreateSubject(LighthouseAppContext context, IReadOnlyList<string> emergencySubjects)
        {
            var config = Options.Create(new AuthorizationConfiguration
            {
                Enabled = true,
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