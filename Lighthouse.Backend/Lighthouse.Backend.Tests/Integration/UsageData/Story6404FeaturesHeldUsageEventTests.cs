using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Written as the text a browser posts and the text that reaches the collector, because a band travels as its
    /// name; a count would identify an instance, which is why only a closed list of bands may leave.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-01")]
    [Category("us-01")]
    [Category("story-6404")]
    public class Story6404FeaturesHeldUsageEventTests : UsageDataCollectorObservationTest
    {
        private const string PortfolioTabOpened = "PortfolioTabOpened";

        private const string TeamTabOpened = "TeamTabOpened";

        private const string PortfolioFeaturesTab = "PortfolioDetail_Features";

        private const string TeamFeaturesTab = "TeamDetail_Features";

        private const string TheBandOnTheWire = "featuresHeld";

        private const string TheBandAtTheCollector = "features_held";

        private const string UsageDataPage = "docs/settings/usagedata.md";

        private const string From400 = "From400";

        private static readonly Dictionary<string, int> EveryBandWithItsInternalNumber = new(StringComparer.Ordinal)
        {
            ["UpTo20"] = 0,
            ["From21To50"] = 1,
            ["From51To100"] = 2,
            ["From101To399"] = 3,
            [From400] = 4,
            ["Unknown"] = 5,
        };

        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, "UpTo20")]
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, "From21To50")]
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, "From51To100")]
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, "From101To399")]
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, From400)]
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab, "Unknown")]
        [TestCase(TeamTabOpened, TeamFeaturesTab, From400)]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("driving_port")]
        [Category("kpi-OUT-6400-openings-by-size")]
        [Category("contract-shape:bounded-change")]
        public async Task A_tab_opening_travels_with_the_band_of_the_Features_its_page_holds(string name, string route, string band)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ATabOpening(name, route, $",\"{TheBandOnTheWire}\":\"{band}\""));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            Assert.That(messages, Has.Count.EqualTo(1), $"The collector was to receive exactly the one opening; the hand-in answered {(int)handedIn.StatusCode}.");

            var properties = messages[0].GetProperty("properties");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), await handedIn.Content.ReadAsStringAsync());
                Assert.That(properties.TryGetProperty(TheBandAtTheCollector, out var travelled) ? travelled.GetString() : null, Is.EqualTo(band));
            }
        }

        // A browser open since before the release does not know the band; its openings still count, without one.
        [TestCase(PortfolioTabOpened, PortfolioFeaturesTab)]
        [TestCase(TeamTabOpened, TeamFeaturesTab)]
        [Category("driving_port")]
        [Category("edge")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_tab_opening_from_a_browser_older_than_the_band_still_counts_and_travels_without_one(string name, string route)
        {
            var token = await ABrowserThatAgreedAsync();

            using var handedIn = await HandInAsync(token, ATabOpening(name, route, string.Empty));
            var messages = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handedIn.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(messages, Has.Count.EqualTo(1));
                Assert.That(messages.Single().GetProperty("properties").TryGetProperty(TheBandAtTheCollector, out _), Is.False,
                    "An opening without a band must not travel with an invented one.");
            }
        }

        // Never a count, never anything outside the six bands, and never a band's internal number, because a band
        // travels by its name: each of these refuses the whole batch.
        [TestCase("920")]
        [TestCase("6")]
        [TestCase("-1")]
        [TestCase("4")]
        [TestCase("0")]
        [TestCase("\"920\"")]
        [TestCase("\"Lots\"")]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task A_tab_opening_carrying_anything_but_one_of_the_six_bands_is_refused(string value)
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, ATabOpening(PortfolioTabOpened, PortfolioFeaturesTab, $",\"{TheBandOnTheWire}\":{value}"));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        // Only the two tab openings may say how large their page is.
        [TestCase("TeamCreated", "")]
        [TestCase("WorkTrackingSystemConnected", ",\"workTrackingSystem\":\"Jira\"")]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("driving_port")]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Any_other_event_carrying_a_band_is_refused(string name, string whatItCarries)
        {
            var token = await ABrowserThatAgreedAsync();

            using var refused = await HandInAsync(token, AnEvent(name, whatItCarries + $",\"{TheBandOnTheWire}\":\"{From400}\""));

            await ThenItIsRefusedAndNothingIsForwarded(refused);
        }

        /// <summary>
        /// A band travels by its name; the number behind each name is the internal value it is stored as, and it must
        /// never move once released, or bands stored before a release would read as another band after it.
        /// </summary>
        [Test]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("kpi-OUT-6400-openings-by-size")]
        [Category("contract-shape:pure-function")]
        public void The_six_bands_travel_by_name_and_each_keeps_the_internal_number_it_was_released_with()
        {
            var vocabulary = typeof(Backend.Program).Assembly.GetTypes()
                .FirstOrDefault(type => type.IsEnum && type.Name.Equals("UsageDataFeaturesHeld", StringComparison.Ordinal));

            Assert.That(vocabulary, Is.Not.Null, "There is no closed list of bands for how many Features a page holds.");

            var named = Enum.GetNames(vocabulary!).ToDictionary(band => band, band => (int)Enum.Parse(vocabulary!, band), StringComparer.Ordinal);
            Assert.That(named, Is.EquivalentTo(EveryBandWithItsInternalNumber));
        }

        // Data leaving that the page does not describe is data nobody was told about.
        [Test]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("contract-shape:bounded-change")]
        public void The_usage_data_page_names_every_band_and_says_the_number_itself_never_travels()
        {
            var page = TheUsageDataPage();

            using (Assert.EnterMultipleScope())
            {
                foreach (var band in EveryBandWithItsInternalNumber.Keys)
                {
                    Assert.That(page, Does.Contain($"`{band}`"), $"The usage data page does not name the band {band}.");
                }

                Assert.That(page, Does.Contain("Never the number itself"));
            }
        }

        // --- Steps ---

        private static string ATabOpening(string name, string route, string parts)
            => AnEvent(name, $",\"route\":\"{route}\"{parts}");

        private static string AnEvent(string name, string parts)
            => $"{{\"events\":[{{\"name\":\"{name}\"{parts},\"offsetMs\":0,\"sequence\":0}}]}}";

        private async Task ThenItIsRefusedAndNothingIsForwarded(HttpResponseMessage refused)
        {
            var forwarded = EveryMessageIn(await EverythingTheCollectorReceived());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(forwarded, Is.Empty);
            }
        }

        private static List<JsonElement> EveryMessageIn(string sent)
        {
            if (string.IsNullOrEmpty(sent))
            {
                return [];
            }

            using var document = JsonDocument.Parse(sent);
            return [.. document.RootElement.EnumerateArray().Select(message => message.Clone())];
        }

        private static string TheUsageDataPage()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the docs read.");
            var repositoryRoot = Directory.GetParent(directory!.FullName)!.FullName;

            return File.ReadAllText(Path.Combine(repositoryRoot, UsageDataPage.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
