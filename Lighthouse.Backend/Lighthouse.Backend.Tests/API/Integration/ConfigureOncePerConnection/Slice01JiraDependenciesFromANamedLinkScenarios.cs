using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Priya Raman's teams record that one Feature waits on another with Jira's "Dependency" link
    /// (<c>depends on</c> / <c>is depended on by</c>), and Features → Dependencies has been empty, because
    /// Lighthouse reads only <c>is blocked by</c>. She registers the link as an Additional Field and picks it as
    /// the "Payments Platform" Portfolio's Dependency Field; after the next update PAY-112 is shown waiting on
    /// PAY-98. The phrase she names sets the direction, so a name that reads both ways reads nothing and says
    /// so. A Dependency Field naming an ordinary field, or no Dependency Field at all, reads exactly as before.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6300")]
    [Category("slice-01")]
    public class Slice01JiraDependenciesFromANamedLinkScenarios : NamedDependencyLinkAcceptanceTest
    {
        private static readonly string[] PAY98 = ["PAY-98"];

        private static readonly string[] PAY112 = ["PAY-112"];

        private static readonly string[] PAY77 = ["PAY-77"];

        private static readonly string[] PAY77AndPAY60 = ["PAY-60", "PAY-77"];

        private static readonly string[] Nothing = [];

        private static readonly string[] BothDependencyPhrases = ["depends on", "is depended on by"];

        private static readonly string[] PAY50 = ["PAY-50"];

        private static readonly string[] OnlyTheTwoWayFinding = ["NamedLinkPointsBothWays"];

        private static readonly DependencySource[] ReadFromATrackerLink = [DependencySource.TrackerLink];

        private static readonly DependencySource[] ReadFromThePortfoliosField = [DependencySource.PortfolioField];

        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.1")]
        [Category("walking_skeleton")]
        [Category("driving_port")]
        [Category("kpi")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Feature_waits_on_what_its_named_one_way_link_points_at()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "depends on", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY98));
                Assert.That(await WhatTheFeatureWaitsOn("PAY-98"), Is.EqualTo(Nothing),
                    "The link is served from both ends; reading the far end too would record one wait twice, pointing the wrong way.");
                Assert.That(await WhereTheFeaturesDependenciesWereReadFrom("PAY-112"), Is.EqualTo(ReadFromATrackerLink),
                    "A link the Portfolio named is still a link somebody drew in the tracker.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.1")]
        [Category("contract-shape:bounded-change")]
        public async Task The_other_ends_phrase_reads_the_other_direction()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "depends on", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("is depended on by");

            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-98"), Is.EqualTo(PAY112));
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(Nothing));
            }
        }

        // Kenji Watanabe's instance renamed the inward end of Blocks to "waits for" and he types it with
        // capitals: the same result "is blocked by" gives on a stock instance.
        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.2")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_renamed_phrase_typed_in_any_case_is_read_like_the_stock_one()
        {
            JiraRenamedTheBlocksInwardPhraseTo("waits for");
            JiraHoldsTheFeatures("PAY-140", "PAY-77");
            OnJira("PAY-140", "waits for", "PAY-77");
            PaymentsPlatformNamesAsItsDependencyField("Waits For");

            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-140"), Is.EqualTo(PAY77));
                Assert.That(await WhatTheFeatureWaitsOn("PAY-77"), Is.EqualTo(Nothing));
            }
        }

        // Field lookup runs first everywhere, so no reference that reads a field today changes meaning.
        [Test]
        [Category("us-01")]
        [Category("ac-1.2")]
        [Category("edge")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_real_field_carrying_the_name_of_a_link_phrase_is_read_as_the_field()
        {
            JiraDefinesTheField("depends on");
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-77");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJiraTheFieldOf("PAY-112", "depends on", "PAY-77");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();

            Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY77),
                "A field that happens to carry a link's name was read as a field before this change and must stay one.");
        }

        // Ana Souza names the type, "Dependency", which reads both ways.
        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_link_types_own_name_reads_nothing_and_Refresh_History_names_both_phrases()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "depends on", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("Dependency");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(Nothing));
                Assert.That(await WhatTheFeatureWaitsOn("PAY-98"), Is.EqualTo(Nothing));
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1), "One finding per refresh, never one per Feature.");
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("NamedLinkPointsBothWays"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("DependencyField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo("Dependency"));
                Assert.That(shown.TheFirstFinding?.Seen ?? Array.Empty<string>(), Is.EquivalentTo(BothDependencyPhrases),
                    "The two phrases are what the admin chooses between, so the finding hands them over.");
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.3")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_phrase_both_ends_share_reads_nothing_and_says_so()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "relates to", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("relates to");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(Nothing));
                Assert.That(shown.Findings?.Select(finding => finding.Kind), Is.EqualTo(OnlyTheTwoWayFinding));
            }
        }

        [Test]
        [Ignore(PendingEpic6303.Slice01)]
        [Category("us-01")]
        [Category("ac-1.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Naming_a_link_replaces_is_blocked_by_for_that_Portfolio()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJira("PAY-112", "is blocked by", "PAY-50");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();

            Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY98),
                "One link type replaces the default; adding the two together would leave nobody able to say where an edge came from.");
        }

        // Lukas Brandt's Dependency Field names the ordinary text field "Blocked By Keys".
        [Test]
        [Category("us-01")]
        [Category("ac-1.5")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_naming_an_ordinary_field_reads_its_keys_exactly_as_before()
        {
            JiraDefinesTheField("Blocked By Keys");
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-77", "PAY-60");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJiraTheFieldOf("PAY-112", "Blocked By Keys", "PAY-77; PAY-60");
            PaymentsPlatformNamesAsItsDependencyField("Blocked By Keys");

            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY77AndPAY60));
                Assert.That(await WhereTheFeaturesDependenciesWereReadFrom("PAY-112"), Is.EqualTo(ReadFromThePortfoliosField));
            }
        }

        [Test]
        [Category("us-01")]
        [Category("ac-1.5")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_naming_nothing_still_reads_is_blocked_by()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJira("PAY-112", "is blocked by", "PAY-50");

            await PaymentsPlatformRefreshes();

            Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY50));
        }

        // The links already arrive with every Feature, so naming one costs the instance nothing. The
        // baseline is the same Portfolio on the same connection, with the link registered and not named,
        // and both readings are of a refresh that downloads everything.
        [Test]
        [Category("us-01")]
        [Category("ac-1.6")]
        [Category("kpi")]
        [Category("active-guard")]
        [Category("contract-shape:bounded-change")]
        public async Task Naming_a_link_costs_the_Jira_instance_no_extra_request()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "depends on", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformIsInSteadyUse();
            var withTheLinkNamed = RequestsJiraAnsweredDuringTheLastRefresh;

            await PaymentsPlatformStopsNamingADependencyField();
            await PaymentsPlatformRefreshes();
            var withNothingNamed = RequestsJiraAnsweredDuringTheLastRefresh;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withNothingNamed, Is.GreaterThan(0), "positive control: the refresh naming nothing reached Jira at all");
                Assert.That(withTheLinkNamed, Is.LessThanOrEqualTo(withNothingNamed),
                    "A refresh naming the link asks Jira for no more than the same refresh naming nothing.");
            }
        }
    }
}
