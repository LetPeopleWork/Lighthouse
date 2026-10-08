using System.Net;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Every event this instance forwards says which surface it came from: the browser, the lh command
    /// line, or an MCP server. A batch that names no source is a browser's, so today's web page and every
    /// tab still holding an old bundle are counted as before. A client names its own source, and a source
    /// outside the three is refused. The state endpoint tells a client which sources this Lighthouse
    /// labels, so a client never sends to one that would count it as a browser.
    ///
    /// Black box over HTTP for the reason the pipe fixture beside this one gives: a pending scenario that
    /// named a type the slice has not added yet would stop the whole test assembly from building. Every
    /// source travels here as the text a client puts on the wire.
    ///
    /// Driving ports: the events, consent and state endpoints, and the usage data page the consent links to.
    /// Observed through the recorder in front of every outbound client, so "the collector received" is what
    /// actually left the instance.
    ///
    /// The three guards are active: they hold today and must keep holding. Every other scenario ships
    /// [Ignore]d; DELIVER un-ignores one at a time, and each is one TDD cycle.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5733-opt-in-usage-data")]
    [Category("story-6193-usage-data-from-clients")]
    public partial class Story6193EveryEventSaysItsSourceTest
    {
        // @US-01 @driving_port @real-io @guard @contract-shape:unbounded-preservation
        // Green today and green after the slice: the web sends no source and must keep being taken in.
        [Test]
        public async Task Todays_browser_batch_is_still_taken_in_and_forwarded()
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, ABatchNamingNoSource(ForecastRun));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(forwarded.Select(message => message.Event), Is.EqualTo(OneForecastRun),
                    "a batch shaped exactly as today's web page sends it was not forwarded, so the slice "
                    + "would break every browser that has not reloaded since the upgrade");
            }
        }

        // @US-01 @driving_port @real-io @kpi @contract-shape:bounded-change
        // KPI-1: a Browser count is browsers only, and the web needs no change to stay in it.
        [Test]
        [Ignore(PendingSlice01)]
        [TestCase(SourceLeftOut)]
        [TestCase(SourceSentAsNull)]
        public async Task A_browser_batch_that_names_no_source_reaches_the_collector_labelled_Browser(string howTheSourceIsMissing)
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, ABatchFrom(howTheSourceIsMissing, ForecastRun));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                ThenEveryMessageCarriesTheSource(forwarded, Browser);
            }
        }

        // @US-01 @driving_port @real-io @kpi @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice01)]
        [TestCase(Cli)]
        [TestCase(Mcp)]
        public async Task A_client_batch_reaches_the_collector_labelled_with_the_source_it_declared(string declared)
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, ABatchDeclaring(declared, TeamRefreshed));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                ThenEveryMessageCarriesTheSource(forwarded, declared);
            }
        }

        // @US-01 @driving_port @real-io @boundary @contract-shape:bounded-change
        // Names are read whatever their case, as every other closed choice on this endpoint is.
        [Test]
        [Ignore(PendingSlice01)]
        public async Task A_source_named_in_lower_case_is_read_as_the_source_it_names()
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, ABatchFrom("\"cli\"", TeamRefreshed));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                ThenEveryMessageCarriesTheSource(forwarded, Cli);
            }
        }

        // @US-01 @driving_port @real-io @kpi @contract-shape:bounded-change
        // KPI-1, north star: one source per batch, written on every message the batch becomes.
        [Test]
        [Ignore(PendingSlice01)]
        public async Task Every_message_a_batch_becomes_carries_the_source_of_that_batch()
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(
                token, ABatchDeclaring(Mcp, ForecastRun, TeamRefreshed, PortfolioRefreshed));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(forwarded, Has.Count.EqualTo(3),
                    "three events were handed in under a live grant, so three messages should have left");
                ThenEveryMessageCarriesTheSource(forwarded, Mcp);
            }
        }

        // @US-01 @driving_port @real-io @error @contract-shape:bounded-change
        // Nothing but our own page and our own clients posts here, so an unknown source is our bug.
        [Test]
        [Ignore(PendingSlice01)]
        [TestCase("\"Shell\"")]
        [TestCase("7")]
        [TestCase("\"\"")]
        [TestCase("true")]
        [TestCase("[\"Cli\"]")]
        public async Task A_source_outside_the_three_is_refused_and_nothing_from_its_batch_is_forwarded(string sourceOnTheWire)
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, ABatchFrom(sourceOnTheWire, TeamRefreshed));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                    "a source that is none of Browser, Cli and Mcp was taken in; it would land in the "
                    + "census as a fourth surface, or worse, under one of the three it is not");
                Assert.That(forwarded, Is.Empty);
            }
        }

        // @US-01 @driving_port @real-io @error @guard @contract-shape:unbounded-preservation
        // The administrator's stop is the server's guarantee, so no client has to check before it sends.
        // Active: it holds today, when a declared source is ignored, and must keep holding once it is read.
        [Test]
        [TestCase(Cli)]
        [TestCase(Mcp)]
        public async Task The_administrators_stop_holds_whatever_the_source(string declared)
        {
            var token = await ABrowserThatAgreedAsync();
            StoreTheVeto(engaged: true);

            using var answer = await HandInAsync(token, ABatchDeclaring(declared, TeamRefreshed));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent),
                    "refusing outright would tell the caller something about this instance");
                Assert.That(forwarded, Is.Empty,
                    "the System Admin stopped usage data, and an event from a client left anyway");
            }
        }

        // @US-01 @driving_port @real-io @error @guard @contract-shape:unbounded-preservation
        // Active for the same reason: declaring a client source must never open a way past the gate.
        [Test]
        [TestCase(WithNoToken)]
        [TestCase(WithATokenNeverMintedHere)]
        [TestCase(WithAWithdrawnToken)]
        public async Task A_client_without_a_live_grant_sends_nothing_whatever_source_it_declares(string whichToken)
        {
            var token = await ATokenThatIs(whichToken);

            using var answer = await HandInAsync(token, ABatchDeclaring(Cli, ForecastRun));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(forwarded, Is.Empty,
                    "declaring a client source opened a way past the gate a browser could not take");
            }
        }

        // @US-01 @driving_port @real-io @version-skew @contract-shape:pure-function
        // A version fact, the same for every caller, so it tells an anonymous caller nothing about anyone.
        [Test]
        [TestCase(WithNoToken)]
        [TestCase(WithATokenNeverMintedHere)]
        [TestCase(WithAGrantedToken)]
        public async Task The_state_tells_any_caller_which_sources_this_Lighthouse_labels(string whichToken)
        {
            var token = await ATokenThatIs(whichToken);

            var accepted = await TheSourcesTheStateSaysThisLighthouseLabels(token);

            Assert.That(accepted, Is.EqualTo(EveryLabelledSource),
                "a client reads this list before it asks anybody; without it a client stays silent "
                + "on this Lighthouse, and with a different list per caller it would be telling one "
                + "caller something about another");
        }

        // @US-01 @driving_port @real-io @contract-shape:bounded-change
        [Test]
        public async Task The_state_adds_the_labelled_sources_and_nothing_else_to_what_it_already_said()
        {
            var fields = await TheFieldsTheStateCarries(token: null);

            Assert.That(fields, Is.EquivalentTo(EverythingTheStateCarriesOnceItLabelsSources),
                "the state is served to anyone who can reach the instance; a field beyond the labelled "
                + "sources is a new disclosure nobody decided on");
        }

        // @US-01 @driving_port @real-io @adapter-integration @contract-shape:bounded-change
        // The cross-repository contract, provider side: the exact body the lh command line sends for a
        // Refinement-day vote that made a Work Item Ready. The clients' own test asserts lh sends exactly
        // this file, so a drift on either side fails in the repository that caused it.
        [Test]
        [Ignore(PendingSlice01)]
        public async Task The_batch_lh_sends_for_a_vote_is_taken_in_and_forwarded_as_lh_declared_it()
        {
            var token = await ABrowserThatAgreedAsync();

            using var answer = await HandInAsync(token, TheFixture(BatchLhSendsForAVoteThatMadeReady));
            var forwarded = await WhatTheCollectorWasHanded();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(forwarded, Is.EqualTo(TheVoteLhReported),
                    "the body lh sends was not forwarded as two events from the command line, both "
                    + "cast on the Team's Refinement day");
            }
        }

        // @US-01 @driving_port @real-io @adapter-integration @contract-shape:pure-function
        // The other half of the contract: what a client parses after its grant. The clients keep a copy of
        // this file and parse exactly it.
        [Test]
        [Ignore(PendingSlice01)]
        public async Task The_state_a_client_reads_after_its_grant_is_the_one_the_clients_parse()
        {
            var token = await ABrowserThatAgreedAsync();

            var served = await TheStateBodyServedTo(token);

            Assert.That(TheSameJson(served, TheFixture(StateAClientReadsAfterItsGrant)), Is.True,
                $"the state served after a grant no longer matches the fixture the clients parse. Served: {served}");
        }

        // @US-01 @driving_port @contract-shape:pure-function
        // The page the consent links to is the disclosure; it never promises less than the product sends.
        [Test]
        [Ignore(PendingSlice01)]
        public void The_usage_data_page_lists_the_source_among_what_every_event_carries()
        {
            var row = TheRowOfWhatEveryEventCarriesNamed(SourceRowName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row, Is.Not.Null,
                    $"'{EveryEventCarriesHeading}' has no '{SourceRowName}' row, so the page says less "
                    + "than every forwarded event now carries");
                Assert.That(row, Does.Contain(Browser).And.Contain(Cli).And.Contain(Mcp),
                    "the Source row must name each of the three values the collector can receive");
            }
        }
    }
}
