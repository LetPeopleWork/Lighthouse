using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces;
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

        [SetUp]
        public void SetUp()
        {
            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            cryptoService = new Mock<ICryptoService>();
            appContextLogger = new Mock<ILogger<LighthouseAppContext>>();
            serviceLogger = new Mock<ILogger<CurrentUserProfileService>>();
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

        private static ClaimsPrincipal BuildPrincipal(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            return new ClaimsPrincipal(identity);
        }
    }
}
