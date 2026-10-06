using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [TestFixture]
    public class CurrentUserProfileServiceTest
    {
        private DbContextOptions<LighthouseAppContext> options;
        private Mock<ICryptoService> cryptoService;
        private Mock<ILogger<LighthouseAppContext>> appContextLogger;
        private Mock<ILogger<CurrentUserProfileService>> serviceLogger;

        private SqliteConnection connection;

        [SetUp]
        public void SetUp()
        {
            cryptoService = new Mock<ICryptoService>();
            appContextLogger = new Mock<ILogger<LighthouseAppContext>>();
            serviceLogger = new Mock<ILogger<CurrentUserProfileService>>();

            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(connection)
                .Options;

            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            context.Database.EnsureCreated();

            // store connection so it stays open for the test lifetime
            this.connection = connection;
        }

        [TearDown]
        public void TearDown()
        {
            connection?.Dispose();
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_SubClaimPresent_CreatesProfile()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            var subject = new CurrentUserProfileService(context, serviceLogger.Object);

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|abc123"),
                new Claim("name", "Story User"),
                new Claim(ClaimTypes.Email, "story.user@example.com"));

            var profile = await subject.GetOrCreateFromPrincipalAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile!.Subject, Is.EqualTo("auth0|abc123"));
                Assert.That(profile.DisplayName, Is.EqualTo("Story User"));
                Assert.That(profile.Email, Is.EqualTo("story.user@example.com"));
                Assert.That(context.UserProfiles.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_OidFallback_UsesOidAsStableSubject()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            var subject = new CurrentUserProfileService(context, serviceLogger.Object);

            var principal = BuildPrincipal(
                new Claim("oid", "aad-oid-42"),
                new Claim("name", "Oid User"));

            var profile = await subject.GetOrCreateFromPrincipalAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile!.Subject, Is.EqualTo("aad-oid-42"));
                Assert.That(profile.SubjectClaimType, Is.EqualTo("oid"));
            }
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_NoStableSubject_ReturnsNullAndDoesNotPersist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            var subject = new CurrentUserProfileService(context, serviceLogger.Object);

            var principal = BuildPrincipal(
                new Claim("name", "Missing Subject"),
                new Claim(ClaimTypes.Email, "missing.subject@example.com"));

            var profile = await subject.GetOrCreateFromPrincipalAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile, Is.Null);
                Assert.That(context.UserProfiles.Count(), Is.Zero);
            }
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_ExistingProfile_UpdatesDisplayNameAndEmail()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);

            context.UserProfiles.Add(new UserProfile
            {
                Subject = "auth0|existing",
                SubjectClaimType = "sub",
                DisplayName = "Old Name",
                Email = "old@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LastSeenAt = DateTime.UtcNow.AddDays(-1),
            });
            await context.SaveChangesAsync();

            var service = new CurrentUserProfileService(context, serviceLogger.Object);

            var principal = BuildPrincipal(
                new Claim("sub", "auth0|existing"),
                new Claim("name", "New Name"),
                new Claim("email", "new@example.com"));

            var profile = await service.GetOrCreateFromPrincipalAsync(principal, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile, Is.Not.Null);
                Assert.That(profile!.DisplayName, Is.EqualTo("New Name"));
                Assert.That(profile.Email, Is.EqualTo("new@example.com"));
                Assert.That(context.UserProfiles.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_ApiKeyPrincipal_LeavesTheOwnersProfileAsItWas()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            await AnasProfileWithoutADisplayName(context);
            var service = new CurrentUserProfileService(context, serviceLogger.Object);

            var profile = await service.GetOrCreateFromPrincipalAsync(AnasApiKey(), CancellationToken.None);
            var stored = await context.UserProfiles.AsNoTracking().SingleAsync(p => p.Subject == "ana");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile?.DisplayName, Is.Null);
                Assert.That(profile?.Email, Is.EqualTo("ana@example.com"));
                Assert.That(stored.DisplayName, Is.Null);
                Assert.That(stored.Email, Is.EqualTo("ana@example.com"));
            }
        }

        [Test]
        public async Task GetOrCreateFromPrincipalAsync_ApiKeyPrincipalWhoseOwnerHasNoProfile_ReturnsNullAndDoesNotPersist()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            var service = new CurrentUserProfileService(context, serviceLogger.Object);

            var profile = await service.GetOrCreateFromPrincipalAsync(AnasApiKey(), CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile, Is.Null);
                Assert.That(context.UserProfiles.Count(), Is.Zero);
            }
        }

        [Test]
        public async Task AVoteCastWithAnApiKeyIsShownUnderItsOwnerNotUnderTheKey()
        {
            using var context = new LighthouseAppContext(options, cryptoService.Object, appContextLogger.Object);
            await AnasProfileWithoutADisplayName(context);
            var service = new CurrentUserProfileService(context, serviceLogger.Object);
            var principal = AnasApiKey();
            var resolver = new VoterIdentityResolver(
                Mock.Of<IAuthModeResolver>(modes => modes.Resolve() == new RuntimeAuthStatus { Mode = AuthMode.Enabled }),
                Mock.Of<IHttpContextAccessor>(accessor => accessor.HttpContext == new DefaultHttpContext { User = principal }));

            var resolution = await resolver.ForWriteAsync(null, null, () => service.GetOrCreateFromPrincipalAsync(principal, CancellationToken.None));

            Assert.That(resolution.Voter?.DisplayName, Is.EqualTo("ana@example.com"));
        }

        private static async Task AnasProfileWithoutADisplayName(LighthouseAppContext context)
        {
            context.UserProfiles.Add(new UserProfile
            {
                Subject = "ana",
                SubjectClaimType = "sub",
                DisplayName = null,
                Email = "ana@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                LastSeenAt = DateTime.UtcNow.AddDays(-1),
            });
            await context.SaveChangesAsync();
        }

        private static ClaimsPrincipal AnasApiKey()
            => ApiKeyPrincipalFactory.Create(
                new ApiKeyValidationResult
                {
                    IsValid = true,
                    ApiKeyId = 3,
                    OwnerResolutionState = ApiKeyOwnerResolutionState.Resolved,
                    OwnerSubject = "ana",
                },
                "ApiKey");

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }
    }
}
