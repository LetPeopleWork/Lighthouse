using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [TestFixture]
    public class OidcGroupSnapshotWriterTest
    {
        private static readonly DateTimeOffset TheClocksInstant = new(2026, 7, 27, 23, 30, 0, TimeSpan.Zero);

        private static readonly string[] Groups = ["lighthouse-admins", "team-blue"];

        private readonly FakeTimeProvider time = new(TheClocksInstant);

        private SqliteConnection connection;
        private DbContextOptions<LighthouseAppContext> options;

        [SetUp]
        public void SetUp()
        {
            time.SetLocalTimeZone(TestZones.FarAheadOfUtc);

            connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            options = new DbContextOptionsBuilder<LighthouseAppContext>()
                .UseSqlite(connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            connection.Dispose();
        }

        [Test]
        public async Task WriteAsync_UnknownSubject_CreatesItsProfileStampedWithTheClockInstant()
        {
            await WriteAsync("auth0|new", Groups);

            using var context = CreateContext();
            var profile = await context.UserProfiles.SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(profile.Subject, Is.EqualTo("auth0|new"));
                Assert.That(profile.SubjectClaimType, Is.EqualTo("sub"));
                Assert.That(profile.LastKnownGroupClaimValues, Is.EqualTo("[\"lighthouse-admins\",\"team-blue\"]"));
                Assert.That(profile.CreatedAt, Is.EqualTo(TheClocksInstant.UtcDateTime));
                Assert.That(profile.LastSeenAt, Is.EqualTo(TheClocksInstant.UtcDateTime));
            }
        }

        [Test]
        public async Task WriteAsync_KnownSubject_StoresTheGroupsOnItsProfileAndNoOther()
        {
            await SeedProfiles(("auth0|alice", "[]"), ("auth0|bob", "[\"bob-group\"]"));

            await WriteAsync("auth0|alice", Groups);

            using var context = CreateContext();
            var stored = await context.UserProfiles.ToDictionaryAsync(p => p.Subject, p => p.LastKnownGroupClaimValues);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored["auth0|alice"], Is.EqualTo("[\"lighthouse-admins\",\"team-blue\"]"));
                Assert.That(stored["auth0|bob"], Is.EqualTo("[\"bob-group\"]"));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task WriteAsync_BlankSubject_IsRefused(string subject)
        {
            await Assert.ThatAsync(() => WriteAsync(subject, Groups), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public async Task WriteAsync_NoGroupList_IsRefused()
        {
            await Assert.ThatAsync(() => WriteAsync("auth0|alice", null!), Throws.TypeOf<ArgumentNullException>());
        }

        private async Task WriteAsync(string subject, string[] groups)
        {
            using var context = CreateContext();
            var writer = new OidcGroupSnapshotWriter(context, Mock.Of<ILogger<OidcGroupSnapshotWriter>>(), time);
            await writer.WriteAsync(subject, groups, CancellationToken.None);
        }

        private async Task SeedProfiles(params (string Subject, string Groups)[] profiles)
        {
            using var context = CreateContext();
            foreach (var (subject, groups) in profiles)
            {
                context.UserProfiles.Add(new UserProfile { Subject = subject, SubjectClaimType = "sub", LastKnownGroupClaimValues = groups });
            }

            await context.SaveChangesAsync();
        }

        private LighthouseAppContext CreateContext()
        {
            return new LighthouseAppContext(options, Mock.Of<ICryptoService>(), Mock.Of<ILogger<LighthouseAppContext>>());
        }
    }
}
