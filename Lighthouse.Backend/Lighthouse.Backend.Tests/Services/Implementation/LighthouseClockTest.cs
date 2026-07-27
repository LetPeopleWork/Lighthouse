using System.Reflection;
using System.Text.Json;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Bug #5567 - T1 at the unit level. The codebase conflated "store instants in UTC" with
    /// "compute calendar days in UTC". This fixture pins the second: which calendar day the
    /// instance is on, given an instant and a zone that disagree.
    ///
    /// Every instant below is supplied by the test, never read from the wall clock, so the expected
    /// days are fixed literals and the assertions hold on every date a run happens to fall on.
    /// </summary>
    [TestFixture]
    public class LighthouseClockTest
    {
        private const string InstanceTimeZoneConfigKey = "Lighthouse:TimeZone";

        private const string InstanceTimeZoneEnvironmentVariable = "Lighthouse__TimeZone";

        private const string UnresolvableTimeZoneId = "Middle/Earth";

        private static readonly string[] ExpectedInterfaceMembers =
        [
            "Now",
            "ToInstanceDay",
            "Today",
            "TodayAsUtcMidnight",
            "Zone",
        ];

        /// <summary>
        /// RCA section 8-T1. Row 1 and row 2 straddle midnight in opposite directions; row 3 is the
        /// control that must not move. All three are wrong on unmodified HEAD.
        /// </summary>
        private static readonly object[] ZoneBoundaryRows =
        [
            new object[] { "2026-07-27T23:30:00Z", "Europe/Zurich", "2026-07-28" },
            new object[] { "2026-07-28T00:30:00Z", "America/Los_Angeles", "2026-07-27" },
            new object[] { "2026-07-28T00:30:00Z", "UTC", "2026-07-28" },
        ];

        [TestCaseSource(nameof(ZoneBoundaryRows))]
        public void Today_AcrossZoneBoundaries_ReturnsTheInstanceCalendarDay(
            string instant, string timeZoneId, string expectedDay)
        {
            var clock = ClockAt(instant, timeZoneId);

            Assert.That(clock.Today, Is.EqualTo(DateOnly.Parse(expectedDay)));
        }

        /// <summary>
        /// The R1 guard at the source, and the single most important assertion in this step. The
        /// global EF converter applies ToUniversalTime() to values AND to query parameters, so a
        /// local-midnight leaking out of the clock with Kind = Local would be shifted back by the
        /// offset on write and land on the previous UTC day - re-introducing this exact bug through
        /// the persistence layer.
        /// </summary>
        [TestCaseSource(nameof(ZoneBoundaryRows))]
        public void TodayAsUtcMidnight_NeverReturnsLocalKind(
            string instant, string timeZoneId, string expectedDay)
        {
            var clock = ClockAt(instant, timeZoneId);

            var midnight = clock.TodayAsUtcMidnight;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(midnight.Kind, Is.EqualTo(DateTimeKind.Utc));
                Assert.That(DateOnly.FromDateTime(midnight), Is.EqualTo(DateOnly.Parse(expectedDay)));
                Assert.That(midnight.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
            }
        }

        /// <summary>
        /// Finding F - the primitive that lets a stored instant be bucketed on the instance day
        /// rather than the UTC day. An item closed at 22:30Z belongs to the next day in Zurich.
        /// </summary>
        [TestCase("2026-07-27T22:30:00Z", "Europe/Zurich", "2026-07-28")]
        [TestCase("2026-07-28T00:30:00Z", "America/Los_Angeles", "2026-07-27")]
        [TestCase("2026-07-27T22:30:00Z", "UTC", "2026-07-27")]
        public void ToInstanceDay_ReducesAStoredInstantToTheInstanceDay(
            string storedInstant, string timeZoneId, string expectedDay)
        {
            var clock = new LighthouseClock(
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId),
                new FakeTimeProvider(DateTimeOffset.Parse("2000-01-01T12:00:00Z")));

            var instanceDay = clock.ToInstanceDay(
                DateTime.Parse(storedInstant).ToUniversalTime());

            Assert.That(instanceDay, Is.EqualTo(DateOnly.Parse(expectedDay)));
        }

        /// <summary>
        /// Branch 1 of the resolution order. .NET 10 accepts IANA ids on every platform, and
        /// /usr/share/zoneinfo is present in mcr.microsoft.com/dotnet/aspnet:10.0, so no Dockerfile
        /// change is needed for a team to opt in.
        /// </summary>
        [TestCase("Europe/Zurich")]
        [TestCase("America/Los_Angeles")]
        public void ResolutionOrder_ConfiguredIanaIdWins(string configuredId)
        {
            var resolved = LighthouseClock.ResolveInstanceTimeZone(configuredId);

            Assert.That(resolved.Id, Is.EqualTo(configuredId));
        }

        /// <summary>
        /// Branch 2 - the path EVERY shipped instance actually takes, because the key is not shipped
        /// in appsettings.json. An empty or whitespace value behaves as absent. This is the
        /// supported default, not a degraded mode: no warning, no error, no exception.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ResolutionOrder_AbsentConfigResolvesToLocalWithoutComplaint(string? configuredId)
        {
            TimeZoneInfo resolved = null!;

            Assert.DoesNotThrow(() => resolved = LighthouseClock.ResolveInstanceTimeZone(configuredId));
            Assert.That(resolved, Is.EqualTo(TimeZoneInfo.Local));
        }

        /// <summary>Branch 3 - when the host has no usable local zone, fall through to UTC.</summary>
        [Test]
        public void ResolutionOrder_LocalUnavailableFallsThroughToUtc()
        {
            var resolved = LighthouseClock.ResolveInstanceTimeZone(null, () => null);

            Assert.That(resolved, Is.EqualTo(TimeZoneInfo.Utc));
        }

        /// <summary>
        /// The asymmetry with the absent key is deliberate: absent means "no opinion", wrong means
        /// "an opinion that cannot be honoured". Silently downgrading the second to the first is how
        /// this whole bug class hides, so the instance must refuse to come up.
        /// </summary>
        [Test]
        public void UnresolvableTimeZoneId_FailsFastAtStartup()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = rootFactory.WithWebHostBuilder(
                builder => builder.UseSetting(InstanceTimeZoneConfigKey, UnresolvableTimeZoneId));

            var failure = Assert.Catch(() => _ = factory.Services);

            Assert.That(failure!.ToString(), Does.Contain(UnresolvableTimeZoneId));
        }

        /// <summary>
        /// The configured key must reach the registered clock through the real composition root -
        /// otherwise the opt-in that is the entire user-facing benefit of this fix is unreachable.
        /// </summary>
        [Test]
        public void ConfiguredTimeZone_ReachesTheRegisteredClock()
        {
            using var rootFactory = new TestWebApplicationFactory<Program>();
            using var factory = rootFactory.WithWebHostBuilder(
                builder => builder.UseSetting(InstanceTimeZoneConfigKey, "America/Los_Angeles"));

            var clock = factory.Services.GetRequiredService<ILighthouseClock>();

            Assert.That(clock.Zone.Id, Is.EqualTo("America/Los_Angeles"));
        }

        /// <summary>
        /// Lighthouse__TimeZone is the ONLY way a containerised team opts in, so the double
        /// underscore convention has to be proven, not assumed. Exercised through the real
        /// configuration provider and the real ServiceConfig reader rather than through a host, so
        /// the process-global environment variable is set and restored within microseconds.
        /// </summary>
        [Test]
        public void EnvironmentVariableOverride_ReachesTheClockThroughServiceConfig()
        {
            var previous = Environment.GetEnvironmentVariable(InstanceTimeZoneEnvironmentVariable);

            try
            {
                Environment.SetEnvironmentVariable(InstanceTimeZoneEnvironmentVariable, "America/Los_Angeles");

                var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
                var serviceConfig = new ServiceConfig(configuration);

                var clock = new LighthouseClock(
                    LighthouseClock.ResolveInstanceTimeZone(serviceConfig.TimeZone),
                    new FakeTimeProvider(DateTimeOffset.Parse("2026-07-28T00:30:00Z")));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(clock.Zone.Id, Is.EqualTo("America/Los_Angeles"));
                    Assert.That(clock.Today, Is.EqualTo(new DateOnly(2026, 7, 27)));
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable(InstanceTimeZoneEnvironmentVariable, previous);
            }
        }

        /// <summary>
        /// Decision 6 - the key ships ABSENT, exactly like Lighthouse:BaseUrl and
        /// Lighthouse:OAuth:StateSecret. A later well-meaning edit that adds a concrete default has
        /// to argue with this test rather than silently re-zone every instance on upgrade.
        /// </summary>
        [Test]
        public void ShippedAppSettings_ContainNoTimeZoneKey()
        {
            var appSettingsPath = Path.Combine(RepositoryRoot(), "Lighthouse.Backend", "appsettings.json");

            using var appSettings = JsonDocument.Parse(File.ReadAllText(appSettingsPath));

            Assert.That(
                appSettings.RootElement.TryGetProperty("Lighthouse", out _),
                Is.False,
                $"{appSettingsPath} must not ship a Lighthouse section. A concrete TimeZone default there " +
                "would move every containerised instance off UTC on upgrade, unannounced (Bug #5567, decision 6).");
        }

        /// <summary>Now is the existing instant seam, not a second wall clock.</summary>
        [Test]
        public void Now_DelegatesToTheRegisteredTimeProvider()
        {
            var timeProvider = new FakeTimeProvider(DateTimeOffset.Parse("2026-07-27T23:30:00Z"));
            var clock = new LighthouseClock(TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich"), timeProvider);

            var before = clock.Now;
            timeProvider.Advance(TimeSpan.FromHours(2));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(before, Is.EqualTo(DateTimeOffset.Parse("2026-07-27T23:30:00Z")));
                Assert.That(clock.Now, Is.EqualTo(DateTimeOffset.Parse("2026-07-28T01:30:00Z")));
            }
        }

        /// <summary>
        /// The surface is exactly the five members of RCA section 4.1 - in particular there is no
        /// member that hands out a DateTime whose kind is not Utc.
        /// </summary>
        [Test]
        public void Interface_ExposesExactlyTheAgreedSurface()
        {
            var members = typeof(ILighthouseClock)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(member => member is not MethodInfo method || !method.IsSpecialName)
                .Select(member => member.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            Assert.That(members, Is.EqualTo(ExpectedInterfaceMembers));
        }

        /// <summary>The fake has to move the instant and the zone independently - that is its job.</summary>
        [Test]
        public void FakeLighthouseClock_MovesInstantAndZoneIndependently()
        {
            var fake = new FakeLighthouseClock(DateTimeOffset.Parse("2026-07-27T23:30:00Z"));

            var utcDay = fake.Today;
            fake.SetZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Zurich"));
            var zurichDay = fake.Today;
            fake.SetInstant(DateTimeOffset.Parse("2026-07-28T23:30:00Z"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(utcDay, Is.EqualTo(new DateOnly(2026, 7, 27)));
                Assert.That(zurichDay, Is.EqualTo(new DateOnly(2026, 7, 28)));
                Assert.That(fake.Today, Is.EqualTo(new DateOnly(2026, 7, 29)));
            }
        }

        private static LighthouseClock ClockAt(string instant, string timeZoneId)
        {
            return new LighthouseClock(
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId),
                new FakeTimeProvider(DateTimeOffset.Parse(instant)));
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the appsettings scan.");
            return directory!.FullName;
        }
    }
}
