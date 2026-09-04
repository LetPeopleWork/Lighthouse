using Lighthouse.Backend.Data;
using Lighthouse.Backend.Services.Implementation.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The behaviour-settings write path picks what happens when a setting is switched by looking up the
    /// setting's key, and there is no branch anywhere else. That only holds while the lookup can answer
    /// for every setting the product seeds and no setting is claimed twice, which is what this fixture
    /// checks - against the real application, with the real seeders, because a list written by hand
    /// drifts from the registrations the moment somebody adds a setting.
    /// </summary>
    [TestFixture]
    public class OptionalFeatureApplierSeamArchUnitTest
    {
        private TestWebApplicationFactory<Program> factory = null!;

        [SetUp]
        public void Init()
        {
            factory = new TestWebApplicationFactory<Program>();

            using var scope = factory.Services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            factory.Dispose();
        }

        [Test]
        public void NoTwoAppliersAnswerForTheSameSetting()
        {
            var claimedTwice = ClaimedKeys()
                .GroupBy(key => key, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            Assert.That(claimedTwice, Is.Empty,
                "Two appliers answering for one setting means which consequences a switch carries depends on the order the " +
                "container happened to register them in. Claimed more than once: " + string.Join(", ", claimedTwice));
        }

        [Test]
        public void NoApplierNamesASettingNobodySeeds()
        {
            var seeded = SeededKeys();

            var orphans = ClaimedKeys()
                .Where(key => !seeded.Contains(key))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();

            Assert.That(orphans, Is.Empty,
                "An applier answering for a setting the product never seeds can never run, so whatever it promises to do is " +
                "not being done. Usually a renamed or removed setting. Named but not seeded: " + string.Join(", ", orphans));
        }

        // Starts from what the product seeds rather than from what is registered, which is the half the
        // check below cannot see: a setting nobody claims must reach the applier that only stores the
        // value, and not whichever claimed one the lookup happened to land on.
        [Test]
        public void EverySeededSettingReachesTheApplierThatClaimsItOrTheDefaultWhenNoneDoes()
        {
            var registry = Registry();
            var appliers = Appliers();

            var misrouted = new List<string>();

            foreach (var key in SeededKeys().OrderBy(key => key, StringComparer.Ordinal))
            {
                var claimant = appliers.SingleOrDefault(applier => string.Equals(applier.Key, key, StringComparison.Ordinal));
                var expected = claimant?.GetType() ?? typeof(DefaultOptionalFeatureApplier);
                var reached = registry.ApplierFor(key).GetType();

                if (reached != expected)
                {
                    misrouted.Add($"{key} -> {reached.Name}, expected {expected.Name}");
                }
            }

            Assert.That(misrouted, Is.Empty,
                "Every seeded setting has to reach the applier that claims it, and a setting nobody claims has to reach the " +
                "one that stores the value and does nothing else. Either mistake means switching that setting carries " +
                "consequences nobody asked for, or quietly carries none. Misrouted: " + string.Join(", ", misrouted));
        }

        // Two things keep the fallback from becoming a claimant, and neither is obvious from reading it.
        // Its key is empty, so no setting can be named after it; and it is registered only as its own
        // type, so it never joins the lookup the registry builds from the claiming appliers. Give it a
        // real key, or register it as the interface, and it starts answering for a setting whose own
        // applier was written to carry consequences it does not carry.
        [Test]
        public void TheApplierThatOnlyStoresTheValueClaimsNoSettingOfItsOwn()
        {
            using var scope = factory.Services.CreateScope();
            var fallback = scope.ServiceProvider.GetRequiredService<DefaultOptionalFeatureApplier>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fallback.Key, Is.Empty,
                    "The fallback applier names a setting. A setting by that name would resolve to it rather than to whichever " +
                    "applier was written for it, and the switch would appear to work while carrying none of its consequences.");

                Assert.That(Appliers(), Has.None.InstanceOf<DefaultOptionalFeatureApplier>(),
                    "The fallback applier is registered as IOptionalFeatureApplier, so it now sits in the registry's lookup " +
                    "under its own key alongside the claiming appliers, rather than behind them as the answer for settings " +
                    "nobody claimed.");
            }
        }

        [Test]
        public void ASettingIsResolvedToTheApplierThatClaimsIt()
        {
            var registry = Registry();

            var misrouted = Appliers()
                .Where(applier => registry.ApplierFor(applier.Key).GetType() != applier.GetType())
                .Select(applier => $"{applier.Key} -> {registry.ApplierFor(applier.Key).GetType().Name}")
                .OrderBy(entry => entry, StringComparer.Ordinal)
                .ToList();

            Assert.That(misrouted, Is.Empty,
                "The applier a setting reaches must be the one that claims it. Falling through to the default here is the " +
                "shape of the bug this seam exists to prevent: the switch is taken, and the work it was supposed to carry " +
                "with it is quietly skipped. Misrouted: " + string.Join(", ", misrouted));
        }

        private List<IOptionalFeatureApplier> Appliers()
        {
            using var scope = factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetServices<IOptionalFeatureApplier>()];
        }

        private List<string> ClaimedKeys() => [.. Appliers().Select(applier => applier.Key)];

        private HashSet<string> SeededKeys()
        {
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return [.. context.OptionalFeatures.AsNoTracking().Select(feature => feature.Key)];
        }

        private OptionalFeatureApplierRegistry Registry()
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<OptionalFeatureApplierRegistry>();
        }
    }
}
