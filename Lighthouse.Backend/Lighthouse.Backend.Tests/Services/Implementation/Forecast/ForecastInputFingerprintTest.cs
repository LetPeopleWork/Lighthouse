using System.Globalization;
using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.API;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    /// <summary>
    /// The fingerprint decides whether a Portfolio forecast runs at all, so it is tested against both ways it
    /// can be wrong. Too blunt, and a forecast stays stale after an input moved, with nothing to show for it.
    /// Too sensitive, and every round computes again and the skip buys nothing. Each input the simulation is
    /// handed is changed on its own here and has to move the fingerprint; everything that is not an input,
    /// such as the order a dictionary happens to be filled in, has to leave it alone.
    /// </summary>
    [TestFixture]
    [Category("story-6354-skip-unchanged-forecasts")]
    [Category("slice-01")]
    public class ForecastInputFingerprintTest
    {
        private const string Pending = "Story 6354: pending until DELIVER writes the forecast input fingerprint";

        private static readonly string[] OnlyCardVault = ["PAY-88"];

        private static readonly string[] EveryInputTheSimulationIsHanded =
        [
            "FormatVersion",
            "Today",
            "Trials",
            "MostDaysOneSimulatedRunMayCover",
            "MostRunsAtOnce",
            "FeaturesInForecastOrder",
            "TeamsOfTheFeatures",
            "RemainingWorkRows",
            "ThroughputAsHanded",
            "ThroughputFilterApplied",
            "ThroughputExcludedSummary",
            "HasSufficientData",
            "FeatureWip",
            "WhatEachFeatureWaitsFor",
        ];

        [Test]
        [Ignore(Pending)]
        [Category("guard")]
        public void Names_every_input_the_simulation_is_handed_and_nothing_else()
        {
            Assert.That(ForecastInputFingerprint.RegisteredMembers, Is.EqualTo(EveryInputTheSimulationIsHanded),
                "A forecast input was added or dropped without deciding its place in the fingerprint.");
        }

        /// <summary>
        /// A new simulation limit would otherwise reach the run without reaching the fingerprint, and a forecast
        /// computed under the old limit would be confirmed under the new one. The cores reach the run only
        /// through how many runs go at once.
        /// </summary>
        [Test]
        [Ignore(Pending)]
        [Category("guard")]
        public void Every_simulation_limit_is_one_of_its_inputs()
        {
            var limitsTheRunReads = typeof(ForecastSimulationLimits)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .Where(name => name != nameof(ForecastSimulationLimits.CoresSeen));

            Assert.That(limitsTheRunReads.Except(ForecastInputFingerprint.RegisteredMembers), Is.Empty);
        }

        [Test]
        [Ignore(Pending)]
        public void The_same_inputs_built_twice_give_the_same_fingerprint()
        {
            Assert.That(
                ForecastInputFingerprint.Of(new APortfolioForecast().Inputs()),
                Is.EqualTo(ForecastInputFingerprint.Of(new APortfolioForecast().Inputs())));
        }

        /// <summary>The stored column has no declared length, so the value has to be a digest, not the inputs written out.</summary>
        [Test]
        [Ignore(Pending)]
        public void Is_a_short_lower_case_digest_however_many_Features_there_are()
        {
            var aLargePortfolio = new APortfolioForecast { ExtraFeatures = 400 };

            Assert.That(ForecastInputFingerprint.Of(aLargePortfolio.Inputs()), Does.Match("^[0-9a-f]{32}$"));
        }

        /// <summary>
        /// Teams, rows and throughput carry no meaning in the order they are handed over, and EF returns them in
        /// whatever order the provider produced. Reading that order would make rounds compute at random.
        /// </summary>
        [Test]
        [Ignore(Pending)]
        public void Teams_rows_and_throughput_handed_in_another_order_give_the_same_fingerprint()
        {
            var handedInAnotherOrder = new APortfolioForecast { EveryUnorderedCollectionReversed = true };

            Assert.That(
                ForecastInputFingerprint.Of(handedInAnotherOrder.Inputs()),
                Is.EqualTo(ForecastInputFingerprint.Of(new APortfolioForecast().Inputs())));
        }

        /// <summary>An instance running under a Turkish locale must not read its own fingerprints as changed.</summary>
        [Test]
        [Ignore(Pending)]
        public void Reads_the_same_whatever_culture_the_instance_runs_under()
        {
            var forecast = new APortfolioForecast { FssExcludedSummary = "2 Items excluded: Bug, Incident" };
            var invariant = ForecastInputFingerprint.Of(forecast.Inputs());

            var culture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                Assert.That(ForecastInputFingerprint.Of(forecast.Inputs()), Is.EqualTo(invariant));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [TestCaseSource(nameof(EachInputChangedOnItsOwn))]
        [Ignore(Pending)]
        public void One_input_changed_on_its_own_changes_the_fingerprint(string member, Action<APortfolioForecast> startingPoint, Action<APortfolioForecast> change)
        {
            var unchanged = new APortfolioForecast();
            startingPoint(unchanged);
            var before = ForecastInputFingerprint.Of(unchanged.Inputs());

            var changed = new APortfolioForecast();
            startingPoint(changed);
            change(changed);

            Assert.That(ForecastInputFingerprint.Of(changed.Inputs()), Is.Not.EqualTo(before),
                $"Changing {member} alone left the fingerprint as it was, so the forecast would not have been computed again.");
        }

        [Test]
        [Ignore(Pending)]
        [Category("guard")]
        public void Every_input_has_a_case_above_that_changes_it_on_its_own()
        {
            var changedByACase = EachInputChangedOnItsOwn().Select(testCase => (string)testCase.Arguments[0]!).ToHashSet();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ForecastInputFingerprint.RegisteredMembers, Is.Not.Empty, "The fingerprint names no inputs at all.");
                Assert.That(
                    ForecastInputFingerprint.RegisteredMembers.Where(member => member != "FormatVersion").Except(changedByACase),
                    Is.Empty);
            }
        }

        /// <summary>
        /// The dependency cases above only mean something if the starting dependency is one the forecast acts
        /// on, and each change makes it one the forecast no longer acts on, or acts on differently.
        /// </summary>
        [Test]
        [Category("guard")]
        public void The_dependency_the_cases_start_from_is_one_the_forecast_waits_on()
        {
            var waitsOn = new APortfolioForecast();
            RefundsWaitsForCardVault(waitsOn);
            var ignored = new APortfolioForecast { PortfolioIgnoresDependencies = true };
            RefundsWaitsForCardVault(ignored);
            var cannotBeForecast = new APortfolioForecast { CardVaultCanBeForecast = false };
            RefundsWaitsForCardVault(cannotBeForecast);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(waitsOn.Inputs().Waits.Of("PAY-90"), Is.EqualTo(OnlyCardVault));
                Assert.That(ignored.Inputs().Waits.Of("PAY-90"), Is.Empty);
                Assert.That(cannotBeForecast.Inputs().Waits.Of("PAY-90"), Is.Empty);
            }
        }

        private static IEnumerable<TestCaseData> EachInputChangedOnItsOwn()
        {
            yield return Case("Today", "the day has turned", forecast => forecast.Today = forecast.Today.AddDays(1));
            yield return Case("Trials", "one more run per forecast", forecast => forecast.Trials += 1);
            yield return Case("MostDaysOneSimulatedRunMayCover", "a run may cover one more day", forecast => forecast.MostDays += 1);
            yield return Case("MostRunsAtOnce", "the instance was given more cores", forecast => forecast.Cores = 8);
            yield return Case("FeaturesInForecastOrder", "two Features swapped order", forecast => forecast.FirstTwoFeaturesSwapped = true);
            yield return Case("FeaturesInForecastOrder", "a Feature left the Portfolio", forecast => forecast.FraudRulesInScope = false);
            yield return Case("TeamsOfTheFeatures", "another Team's throughput is read", forecast => forecast.OrionIsRead = true);
            yield return Case("RemainingWorkRows", "an item of a Feature was finished", forecast => forecast.CardVaultRemainingForFss = 2);
            yield return Case("RemainingWorkRows", "the same work moved from one Team to the other", forecast =>
            {
                forecast.RefundsRemainingForFss = 3;
                forecast.RefundsRemainingForAtlas = 1;
            });
            yield return Case("ThroughputAsHanded", "a day delivered one item fewer", forecast => forecast.FssDailyThroughput[4] = 0);
            yield return Case("ThroughputAsHanded", "a blackout took a day out of the history", forecast => forecast.FssDailyThroughput = forecast.FssDailyThroughput[..^1]);
            yield return Case("ThroughputAsHanded", "the throughput window grew", forecast => forecast.FssDailyThroughput = [.. forecast.FssDailyThroughput, 1, 0, 1]);
            yield return Case("ThroughputFilterApplied", "the forecast filter is now applied", forecast => forecast.FssFilterApplied = true);
            yield return Case("ThroughputExcludedSummary", "the filter now excludes something", forecast => forecast.FssExcludedSummary = "2 Items excluded: Bug");
            yield return Case("HasSufficientData", "the Team no longer has enough data", forecast => forecast.FssHasSufficientData = false);
            yield return Case("FeatureWip", "a Team's Feature WIP moved from 2 to 3", forecast => forecast.FssFeatureWip = 3);
            yield return Case("WhatEachFeatureWaitsFor", "a Feature gained a dependency", forecast => forecast.RefundsWaitsFor = ["PAY-88"]);
            yield return Case("WhatEachFeatureWaitsFor", "a dependency now points at another Feature",
                RefundsWaitsForCardVault, forecast => forecast.RefundsWaitsFor = ["PAY-97"]);
            yield return Case("WhatEachFeatureWaitsFor", "the Portfolio started ignoring dependencies",
                RefundsWaitsForCardVault, forecast => forecast.PortfolioIgnoresDependencies = true);
            yield return Case("WhatEachFeatureWaitsFor", "the Feature waited on can no longer be forecast",
                RefundsWaitsForCardVault, forecast => forecast.CardVaultCanBeForecast = false);
        }

        private static void RefundsWaitsForCardVault(APortfolioForecast forecast) => forecast.RefundsWaitsFor = ["PAY-88"];

        private static TestCaseData Case(string member, string change, Action<APortfolioForecast> apply)
            => Case(member, change, _ => { }, apply);

        private static TestCaseData Case(string member, string change, Action<APortfolioForecast> startingPoint, Action<APortfolioForecast> apply)
            => new TestCaseData(member, startingPoint, apply).SetName($"One input changed on its own changes the fingerprint: {change}");

        /// <summary>
        /// "Payments Platform" as the simulation would be handed it: three Features in order, two Teams, and
        /// one dependency only when a case asks for one. Every knob is a plain value, so a case changes exactly
        /// one input and nothing else.
        /// </summary>
        public sealed class APortfolioForecast
        {
            public DateOnly Today { get; set; } = new(2026, 10, 23);

            public int Trials { get; set; } = 200;

            public int MostDays { get; set; } = 60;

            public int Cores { get; set; } = 4;

            public bool FirstTwoFeaturesSwapped { get; set; }

            public bool FraudRulesInScope { get; set; } = true;

            public bool OrionIsRead { get; set; }

            public int CardVaultRemainingForFss { get; set; } = 3;

            public int RefundsRemainingForFss { get; set; } = 2;

            public int RefundsRemainingForAtlas { get; set; } = 2;

            public int[] FssDailyThroughput { get; set; } = [1, 0, 1, 1, 1, 0, 1, 1, 0, 1];

            public bool FssFilterApplied { get; set; }

            public string? FssExcludedSummary { get; set; }

            public bool FssHasSufficientData { get; set; } = true;

            public int FssFeatureWip { get; set; } = 2;

            public string[] RefundsWaitsFor { get; set; } = [];

            public bool PortfolioIgnoresDependencies { get; set; }

            public bool CardVaultCanBeForecast { get; set; } = true;

            public int ExtraFeatures { get; set; }

            public bool EveryUnorderedCollectionReversed { get; set; }

            public ForecastInputs Inputs()
            {
                var portfolio = new Portfolio { Id = 7, Name = "Payments Platform", IgnoreDependencies = PortfolioIgnoresDependencies };
                var fss = new Team { Id = 11, Name = "FSS", FeatureWIP = FssFeatureWip };
                var atlas = new Team { Id = 12, Name = "Atlas", FeatureWIP = 1 };
                var orion = new Team { Id = 13, Name = "Orion", FeatureWIP = 1 };

                var cardVault = AFeature(88, "PAY-88", portfolio, [(fss, CardVaultRemainingForFss)], CardVaultCanBeForecast);
                var refunds = AFeature(90, "PAY-90", portfolio, [(fss, RefundsRemainingForFss), (atlas, RefundsRemainingForAtlas)], true);
                refunds.ReplaceDependsOnReferences(RefundsWaitsFor.Select(blocker => new FeatureDependencyReference(refunds.Id, blocker, DependencySource.TrackerLink)));

                var features = FirstTwoFeaturesSwapped ? new List<Feature> { refunds, cardVault } : [cardVault, refunds];

                if (FraudRulesInScope)
                {
                    features.Add(AFeature(97, "PAY-97", portfolio, [(fss, 13), (atlas, 12)], true));
                }

                features.AddRange(Enumerable.Range(1, ExtraFeatures)
                    .Select(index => AFeature(1000 + index, $"PAY-{1000 + index}", portfolio, [(fss, index % 7 + 1)], true)));

                var throughput = new List<KeyValuePair<int, ForecastThroughputStatus>>
                {
                    new(fss.Id, new ForecastThroughputStatus(AsHanded(FssDailyThroughput), FssFilterApplied, FssExcludedSummary, FssHasSufficientData)),
                    new(atlas.Id, new ForecastThroughputStatus(AsHanded([1, 1, 0, 1, 0, 1, 1, 0, 1, 1]), false, null)),
                };

                if (OrionIsRead)
                {
                    throughput.Add(new(orion.Id, new ForecastThroughputStatus(AsHanded([0, 1, 1, 0, 1, 0, 0, 1, 1, 1]), false, null)));
                }

                if (EveryUnorderedCollectionReversed)
                {
                    throughput.Reverse();
                    foreach (var feature in features)
                    {
                        feature.FeatureWork.Reverse();
                    }
                }

                return new ForecastInputs(
                    Today,
                    new ForecastSimulationLimits(Trials, MostDays) { CoresSeen = Cores },
                    features,
                    throughput.ToDictionary(pair => pair.Key, pair => pair.Value),
                    WhatTheyWaitFor(features));
            }

            private static Feature AFeature(int id, string referenceId, Portfolio portfolio, (Team Team, int Remaining)[] work, bool canBeForecast)
            {
                var feature = new Feature(work.Select(row => (row.Team, row.Remaining, row.Remaining + 1)))
                {
                    Id = id,
                    ReferenceId = referenceId,
                    Name = referenceId,
                    Order = id.ToString(CultureInfo.InvariantCulture),
                };

                feature.Portfolios.Add(portfolio);

                // A Feature the last forecast could not simulate for one of its Teams cannot be waited on.
                feature.Forecasts = [.. work.Select(row => new WhenForecast { TeamId = row.Team.Id, TotalTrials = canBeForecast ? 200 : 0 })];

                return feature;
            }

            private static RunChartData AsHanded(int[] eachDay) => new(RunChartDataGenerator.GenerateRunChartData(eachDay));

            private static ForecastWaits WhatTheyWaitFor(IReadOnlyCollection<Feature> features)
            {
                var licence = new Mock<ILicenseService>();
                licence.Setup(service => service.CanUsePremiumFeatures()).Returns(true);

                return new WhatTheForecastWaitsFor(new DependencyDecision(new DependencyHonourPolicy(), licence.Object)).Of(features);
            }
        }
    }
}
