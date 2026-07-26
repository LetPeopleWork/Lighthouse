using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER step definitions (Specifications) for Slice 04 — every process-behaviour metric family is
    /// readable over time, not just Throughput. Backend-observable contract (ADR-108): the SHIPPED
    /// <c>process-behavior-over-time</c> action on both metrics controllers is already generic over
    /// <see cref="ProcessBehaviorMetricType"/>, so appending the five remaining families (slice-04 step
    /// 04-02) makes them readable with NO controller change — these scenarios are the verification of
    /// that claim, family by family, through the untouched endpoint.
    ///
    /// Why this is the read PORT rather than the UI: milestone-4's Scenario Outline cannot be driven
    /// through the browser this slice, because the demo backfill deliberately stays Throughput-only
    /// (maintainer decision 2026-07-26) — there is no seeded demo history for the other five families to
    /// plot. The read port is therefore the outermost boundary at which "each family reads its OWN
    /// series" (US-05 AC2) is observable.
    ///
    /// Scope decision D8, recorded rather than left unstated: Feature Size is portfolio-only, and that is
    /// enforced at the frontend toggle (step 04-04) and STRUCTURALLY in the recorder (no team-side read
    /// method exists) — NOT at the wire. A team asking for <c>?type=FeatureSize</c> therefore passes
    /// <c>Enum.IsDefined</c> and gets an honest empty 200, because nothing is ever recorded for a team.
    /// <see cref="Slice04ProcessBehaviorMetricTypesTest.Feature_size_is_a_portfolio_family_and_a_team_asking_for_it_gets_the_honest_empty_state"/>
    /// pins that SHIPPED behaviour instead of changing it.
    ///
    /// Harness caveat (unchanged since slice-02): the harness anchors <c>SyncDay</c> in the past while the
    /// recorder writes <c>RecordedAt = today</c>. This fixture is read-path only and seeds its rows
    /// explicitly, so every expectation is expressed against the SEEDED <c>RecordedAt</c> values — never
    /// against <c>SyncDay</c> and never against today.
    /// </summary>
    public partial class Slice04ProcessBehaviorMetricTypesTest : PercentilesOverTimeAcceptanceTest
    {
        private const int RecordedDayCount = 3;

        /// <summary>
        /// Offsets a neighbour owner's triples away from the owner under test, so a response that leaked
        /// the neighbour's rows is wrong in its NUMBERS as well as its row count.
        /// </summary>
        private const int NeighbourSpread = 7_000;

        private readonly record struct DatedLimits(DateOnly RecordedAt, int Unpl, int Average, int Lnpl);

        // --- Given ---

        private int GivenATeam() => SeedTeam();

        private int GivenAPortfolio() => SeedPortfolio();

        /// <summary>
        /// Persists a run of recorded days for ONE family and returns what was recorded, so the
        /// expectation is the seeded truth rather than a restatement of the production formula.
        /// Every family gets a DIFFERENT triple (see <see cref="ARunOfRecordedDays"/>), which is what
        /// makes a family-blind read fail these scenarios instead of passing them by coincidence.
        /// </summary>
        private List<DatedLimits> GivenPersistedLimits(int ownerId, OwnerType ownerType, ProcessBehaviorMetricType family, int extraSpread = 0)
        {
            var recorded = ARunOfRecordedDays(family, extraSpread);

            // Seeded in REVERSE, exactly as slice-03 does: the endpoint's ascending-date guarantee must be
            // proven by its OrderBy, never inherited from insertion order.
            foreach (var point in Enumerable.Reverse(recorded))
            {
                SeedProcessBehaviorSnapshot(new ProcessBehaviorSnapshot
                {
                    OwnerId = ownerId,
                    OwnerType = ownerType,
                    RecordedAt = point.RecordedAt,
                    MetricType = family,
                    Unpl = point.Unpl,
                    Average = point.Average,
                    Lnpl = point.Lnpl,
                });
            }

            return recorded;
        }

        /// <summary>
        /// A plausible ascending run of weekly recorded days ending on the sync day. The triples are
        /// spread per family so that two families recorded on the SAME owner and the SAME days carry
        /// distinguishable numbers.
        /// </summary>
        private List<DatedLimits> ARunOfRecordedDays(ProcessBehaviorMetricType family, int extraSpread)
        {
            var spread = ((int)family * 100) + extraSpread;
            var points = new List<DatedLimits>();

            for (var index = 0; index < RecordedDayCount; index++)
            {
                var recordedAt = DateOnly.FromDateTime(SyncDay.AddDays(-7 * (RecordedDayCount - 1 - index)));
                points.Add(new DatedLimits(recordedAt, Unpl: spread + 12 + index, Average: spread + 7 + index, Lnpl: spread + 2 + index));
            }

            return points;
        }

        // --- When (the SHIPPED read port, over its real protocol — never the query or repository) ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheDeliveryLeadOpensTheTeamPbcOverTimeWidget(int teamId, ProcessBehaviorMetricType family)
            => GetTeamProcessBehaviorOverTime(teamId, family.ToString());

        private Task<(HttpStatusCode Status, string Body)> WhenTheDeliveryLeadOpensThePortfolioPbcOverTimeWidget(int portfolioId, ProcessBehaviorMetricType family)
            => GetPortfolioProcessBehaviorOverTime(portfolioId, family.ToString());

        // --- Then ---

        private static void ThenTheDatedLimitTripleComesBackOrderedByDate((HttpStatusCode Status, string Body) response, List<DatedLimits> expected)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The shipped process-behavior-over-time endpoint must serve every declared family, with no controller change. Body: {response.Body}");

            var actual = ReadLimitSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual, Has.Count.EqualTo(expected.Count),
                    $"The series must contain exactly this owner's rows for THIS family — no other family's and no other owner's. Body: {response.Body}");
                for (var i = 0; i < expected.Count; i++)
                {
                    Assert.That(actual[i].RecordedAt, Is.EqualTo(expected[i].RecordedAt.ToString("yyyy-MM-dd")),
                        $"Row {i} must be ordered ascending by RecordedAt. Body: {response.Body}");
                    Assert.That(actual[i].Unpl, Is.EqualTo(expected[i].Unpl), $"Row {i} upper natural process limit. Body: {response.Body}");
                    Assert.That(actual[i].Average, Is.EqualTo(expected[i].Average), $"Row {i} average. Body: {response.Body}");
                    Assert.That(actual[i].Lnpl, Is.EqualTo(expected[i].Lnpl), $"Row {i} lower natural process limit. Body: {response.Body}");
                }
            }
        }

        /// <summary>
        /// Both directions of a family pair, so the scenario proves each family reads ITS OWN series
        /// rather than proving one family happens to win the tie.
        /// </summary>
        private static void ThenEachFamilyKeepsItsOwnSeries(
            (HttpStatusCode Status, string Body) response,
            List<DatedLimits> expected,
            (HttpStatusCode Status, string Body) otherResponse,
            List<DatedLimits> otherExpected)
        {
            ThenTheDatedLimitTripleComesBackOrderedByDate(response, expected);
            ThenTheDatedLimitTripleComesBackOrderedByDate(otherResponse, otherExpected);
        }

        private static void ThenTheWidgetGetsTheHonestEmptyState((HttpStatusCode Status, string Body) response)
        {
            var actual = ReadLimitSeries(response.Body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"A declared family with nothing recorded for this owner must still get an honest response, not an error. Body: {response.Body}");
                Assert.That(actual, Is.Empty,
                    $"With nothing recorded the series must be an empty array, never zero-padded and never another owner's or family's rows. Body: {response.Body}");
            }
        }

        private static List<LimitRow> ReadLimitSeries(string body)
        {
            Assert.That(body.TrimStart(), Does.StartWith("["),
                $"The endpoint must return a JSON array, not HTML/other. Body starts: {body[..Math.Min(60, body.Length)]}");

            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement
                .EnumerateArray()
                .Select(element => new LimitRow(
                    element.GetProperty("recordedAt").GetString() ?? string.Empty,
                    element.GetProperty("unpl").GetInt32(),
                    element.GetProperty("average").GetInt32(),
                    element.GetProperty("lnpl").GetInt32()))];
        }

        private readonly record struct LimitRow(string RecordedAt, int Unpl, int Average, int Lnpl);
    }
}
