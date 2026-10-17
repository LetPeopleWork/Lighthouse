namespace Lighthouse.Backend.Tests.API.Integration.ConfigureOncePerConnection
{
    /// <summary>
    /// Priya names <c>depends on</c>, the update finishes, and Dependencies is empty. Settings → System Info
    /// → Refresh History, on the row for Payments Platform's latest update, now says that no Feature carries a
    /// <c>depends on</c> link and that <c>is blocked by</c> and <c>relates to</c> were seen, so she can retype
    /// the phrase or accept that this Portfolio records no such waits. One finding per refresh of the whole
    /// query, never one per Feature, and the same finding as one line in the log.
    ///
    /// A refresh that only downloads what moved looks at too little to say anything was missing, so Refresh
    /// History shows the findings of the latest full download, with its time, until the next full download
    /// replaces them.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6300")]
    [Category("slice-02")]
    public class Slice02NamedLinkThatMatchedNothingScenarios : NamedDependencyLinkAcceptanceTest
    {
        private static readonly string[] TheLinksSeen = ["is blocked by", "relates to"];

        private static readonly string[] TheNothingMatchedFinding = ["NamedLinkMatchedNothing"];

        [Test]
        [Category("us-02")]
        [Category("ac-2.1")]
        [Category("ac-2.2")]
        [Category("error")]
        [Category("kpi")]
        [Category("driving_port")]
        [Category("contract-shape:bounded-change")]
        public async Task Nothing_matched_the_named_link_and_Refresh_History_lists_the_links_that_were_seen()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "relates to", "PAY-98");
            OnJira("PAY-98", "is blocked by", "PAY-50");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1));
                Assert.That(shown.TheFirstFinding?.Kind, Is.EqualTo("NamedLinkMatchedNothing"));
                Assert.That(shown.TheFirstFinding?.Setting, Is.EqualTo("DependencyField"));
                Assert.That(shown.TheFirstFinding?.Named, Is.EqualTo("depends on"));
                Assert.That(shown.TheFirstFinding?.Seen ?? Array.Empty<string>(), Is.EquivalentTo(TheLinksSeen));
                Assert.That(shown.FindingsAsOf, Is.EqualTo(shown.NewestRunAt), "The finding is labelled with the run that found it.");
                Assert.That(WhatTheLastRefreshWarned().Count(line => line.Contains("\"depends on\"", StringComparison.Ordinal)), Is.EqualTo(1),
                    "The log carries the same finding as one aggregated line.");
            }
        }

        [Test]
        [Category("us-02")]
        [Category("ac-2.1")]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public async Task Several_Features_without_the_link_share_one_finding_and_one_line()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-77", "PAY-50");
            OnJira("PAY-112", "relates to", "PAY-50");
            OnJira("PAY-98", "relates to", "PAY-50");
            OnJira("PAY-77", "is blocked by", "PAY-50");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.Findings, Is.Not.Null.And.Count.EqualTo(1), "A finding per Feature buries the rest of the refresh under it.");
                Assert.That(WhatTheLastRefreshWarned().Count(line => line.Contains("\"depends on\"", StringComparison.Ordinal)), Is.EqualTo(1));
            }
        }

        [Test]
        [Category("us-02")]
        [Category("ac-2.1")]
        [Category("contract-shape:bounded-change")]
        public async Task Something_matched_so_Refresh_History_shows_no_finding()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-77");
            OnJira("PAY-112", "depends on", "PAY-98");
            OnJira("PAY-77", "relates to", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            Assert.That(shown.Findings, Is.Not.Null.And.Empty,
                "Refresh History answers with the latest full run's findings, and that run found nothing worth saying.");
        }

        [Test]
        [Category("us-02")]
        [Category("ac-2.1")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Portfolio_whose_Features_carry_no_links_at_all_is_told_nothing()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            Assert.That(shown.Findings, Is.Not.Null.And.Empty, "No links at all is an ordinary Portfolio, as it is today.");
        }

        [Test]
        [Category("us-02")]
        [Category("ac-2.3")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_Dependency_Field_naming_an_ordinary_field_stays_silent()
        {
            JiraDefinesTheField("Blocked By Keys");
            JiraHoldsTheFeatures("PAY-112", "PAY-98");
            OnJira("PAY-112", "relates to", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("Blocked By Keys");

            await PaymentsPlatformRefreshes();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            Assert.That(shown.Findings, Is.Not.Null.And.Empty);
        }

        // A refresh that downloads only what moved reads a handful of Features and cannot tell "nothing
        // carries the link" from "the few that moved do not". The latest full download keeps speaking until
        // another full download has something else to say.
        [Test]
        [Category("us-02")]
        [Category("ac-2.2")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task A_cheap_refresh_keeps_showing_the_full_downloads_finding_and_its_time_until_the_next_full_download()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "relates to", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            var (theLastFullDownloadAt, afterACheapRefresh) = await PaymentsPlatformRefreshesUntilARefreshDownloadsOnlyWhatMoved();

            OnJira("PAY-50", "depends on", "PAY-98");
            await SomethingMakesPaymentsPlatformsNextRefreshDownloadEverything();
            await PaymentsPlatformRefreshes();
            var afterTheNextFullDownload = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(afterACheapRefresh.Findings?.Select(finding => finding.Kind), Is.EqualTo(TheNothingMatchedFinding));
                Assert.That(afterACheapRefresh.FindingsAsOf, Is.EqualTo(theLastFullDownloadAt),
                    "The finding still carries the time of the full download that made it, not of the cheap refresh.");
                Assert.That(afterTheNextFullDownload.NewestRunMode, Is.EqualTo("Full"));
                Assert.That(afterTheNextFullDownload.Findings, Is.Not.Null.And.Empty, "A full download that found the link takes the finding away.");
            }
        }

        // Cheap refreshes are the steady state, so a Portfolio can go on for weeks without another full
        // download. Refresh History keeps only the newest rows; the full download's row must not be the one
        // it lets go of, or the warning vanishes while the Dependency Field still reads nothing.
        [Test]
        [Category("us-02")]
        [Category("ac-2.2")]
        [Category("edge")]
        [Category("contract-shape:bounded-change")]
        public async Task The_finding_still_shows_after_more_cheap_refreshes_than_Refresh_History_keeps()
        {
            JiraHoldsTheFeatures("PAY-112", "PAY-98", "PAY-50");
            OnJira("PAY-112", "relates to", "PAY-98");
            PaymentsPlatformNamesAsItsDependencyField("depends on");

            var (theLastFullDownloadAt, _) = await PaymentsPlatformRefreshesUntilARefreshDownloadsOnlyWhatMoved();

            await PaymentsPlatformRefreshesMoreTimesThanRefreshHistoryKeeps();
            var shown = await WhatRefreshHistoryShowsForPaymentsPlatform();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.NewestRunMode, Is.EqualTo("Delta"));
                Assert.That(shown.Findings?.Select(finding => finding.Kind), Is.EqualTo(TheNothingMatchedFinding));
                Assert.That(shown.FindingsAsOf, Is.EqualTo(theLastFullDownloadAt));
            }
        }
    }
}
