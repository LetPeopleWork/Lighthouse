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
    public class Slice01JiraDependenciesFromANamedLinkScenarios : NamedDependencyLinkAcceptanceTest
    {
        private static readonly string[] PAY98 = ["PAY-98"];

        private static readonly string[] Nothing = [];

        private static readonly string[] PAY50 = ["PAY-50"];

        private static readonly DependencySource[] ReadFromATrackerLink = [DependencySource.TrackerLink];

        [Test]
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

        // Without the link type list a name that is not a field cannot be told from one that matches nothing,
        // and reading it as nothing would rewrite every Feature as waiting on nothing.
        [Test]
        [Category("us-01")]
        [Category("ac-1.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task A_refused_link_type_list_stops_the_refresh_and_leaves_the_stored_dependencies_as_they_were()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJira("PAY-112", "is blocked by", "PAY-50");
            JiraRefusesToListItsLinkTypes();
            PaymentsPlatformsConnectionRegisters("depends on");

            await PaymentsPlatformRefreshes();
            var beforeNamingIt = await WhatTheFeatureWaitsOn("PAY-112");

            await PaymentsPlatformsAdminNamesTheRegisteredFieldAsItsDependencyField();
            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeNamingIt, Is.EqualTo(PAY50), "positive control: a dependency was stored before the link was named");
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY50),
                    "A refresh that cannot tell what the name means must not rewrite what was stored.");
                Assert.That(WhatTheLastRefreshWarned(), Has.Some.Matches<string>(warning => warning.Contains(PaymentsPlatform, StringComparison.Ordinal) && warning.Contains("503", StringComparison.Ordinal)),
                    "Whoever reads the log has to learn which Portfolio stopped and what Jira answered.");
            }
        }

        // A proxy or maintenance page can answer the link type list with 200 and something that is not the
        // list at all. That says no more about what the name means than a refusal does.
        [Test]
        [Category("us-01")]
        [Category("ac-1.4")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task An_unreadable_link_type_list_stops_the_refresh_and_leaves_the_stored_dependencies_as_they_were()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJira("PAY-112", "is blocked by", "PAY-50");
            JiraAnswersItsLinkTypeListUnreadably();
            PaymentsPlatformsConnectionRegisters("depends on");

            await PaymentsPlatformRefreshes();
            var beforeNamingIt = await WhatTheFeatureWaitsOn("PAY-112");

            await PaymentsPlatformsAdminNamesTheRegisteredFieldAsItsDependencyField();
            await PaymentsPlatformRefreshes();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeNamingIt, Is.EqualTo(PAY50), "positive control: a dependency was stored before the link was named");
                Assert.That(await WhatTheFeatureWaitsOn("PAY-112"), Is.EqualTo(PAY50),
                    "A refresh that could not read the link type list must not rewrite what was stored.");
                Assert.That(WhatTheLastRefreshWarned(), Has.Some.Matches<string>(warning => warning.Contains(PaymentsPlatform, StringComparison.Ordinal) && warning.Contains("could not read", StringComparison.Ordinal)),
                    "Whoever reads the log has to learn which Portfolio stopped and that Jira's reply could not be read.");
            }
        }
    }
}
