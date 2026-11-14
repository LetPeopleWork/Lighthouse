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
    public class Slice02NamedLinkThatMatchedNothingScenarios : NamedDependencyLinkAcceptanceTest
    {
        private static readonly string[] TheLinksSeen = ["is blocked by", "relates to"];

        private const string TheNothingMatchedLine = "names 'depends on', and no Feature carries that link";

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
                Assert.That(WhatTheLastRefreshWarned().Count(line => line.Contains(TheNothingMatchedLine, StringComparison.Ordinal)), Is.EqualTo(1),
                    "The log carries the same finding as one aggregated line.");
            }
        }
    }
}
