using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    [TestFixture]
    public class RefreshLogDtoTest
    {
        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void A_row_carries_whether_its_forecast_was_skipped(bool skipped)
        {
            var row = SerialisedRowFor(new RefreshLog { Id = 1, Type = RefreshType.Forecast, EntityId = 7, Skipped = skipped });

            Assert.That(row["skipped"]?.GetValue<bool>(), Is.EqualTo(skipped));
        }

        [Test]
        public void A_computed_row_says_so_explicitly_rather_than_leaving_the_member_out()
        {
            var row = SerialisedRowFor(new RefreshLog { Id = 1, Type = RefreshType.Team, EntityId = 3 });

            Assert.That(row.ContainsKey("skipped"), Is.True);
        }

        [Test]
        public void A_cheap_runs_row_shows_the_findings_and_the_time_of_the_last_full_run_that_recorded_any()
        {
            var fullRun = ARun(1, SyncMode.Full, TheFirstRunAt, [AFinding()]);
            var cheapRun = ARun(2, SyncMode.Delta, TheFirstRunAt.AddHours(1), null);

            var shown = RefreshLogDto.ListFor([fullRun, cheapRun]).Single(row => row.Id == 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.LatestFullRunFindings, Is.EqualTo(new[] { AFinding() }));
                Assert.That(shown.LatestFullRunAt, Is.EqualTo(TheFirstRunAt),
                    "The finding carries the time of the full run that made it, not of the cheap run whose row shows it.");
            }
        }

        [Test]
        public void A_later_full_run_that_found_nothing_replaces_the_findings_of_an_older_one()
        {
            var olderFullRun = ARun(1, SyncMode.Full, TheFirstRunAt, [AFinding()]);
            var newerFullRun = ARun(2, SyncMode.Full, TheFirstRunAt.AddHours(1), []);
            var cheapRun = ARun(3, SyncMode.Delta, TheFirstRunAt.AddHours(2), null);

            var shown = RefreshLogDto.ListFor([olderFullRun, newerFullRun, cheapRun]).Single(row => row.Id == 3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shown.LatestFullRunFindings, Is.Not.Null.And.Empty, "A full run that found nothing takes the finding away.");
                Assert.That(shown.LatestFullRunAt, Is.EqualTo(TheFirstRunAt.AddHours(1)));
            }
        }

        [Test]
        public void The_findings_of_one_entity_are_not_shown_on_the_rows_of_another()
        {
            var portfolioRun = ARun(1, SyncMode.Full, TheFirstRunAt, [AFinding()]);
            var otherPortfolioRun = new RefreshLog { Id = 2, Type = RefreshType.Portfolio, EntityId = 8, Mode = SyncMode.Delta, ExecutedAt = TheFirstRunAt };
            var teamRun = new RefreshLog { Id = 3, Type = RefreshType.Team, EntityId = 7, Mode = SyncMode.Delta, ExecutedAt = TheFirstRunAt };

            var rows = RefreshLogDto.ListFor([portfolioRun, otherPortfolioRun, teamRun]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rows.Single(row => row.Id == 2).LatestFullRunFindings, Is.Null);
                Assert.That(rows.Single(row => row.Id == 3).LatestFullRunFindings, Is.Null);
            }
        }

        [Test]
        public void An_entity_none_of_whose_runs_recorded_findings_sends_its_row_without_the_two_members()
        {
            var row = SerialisedRowFor(ARun(1, SyncMode.Delta, TheFirstRunAt, null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.ContainsKey("latestFullRunFindings"), Is.False);
                Assert.That(row.ContainsKey("latestFullRunAt"), Is.False);
            }
        }

        private static readonly DateTime TheFirstRunAt = new(2026, 9, 1, 9, 0, 0, DateTimeKind.Utc);

        private static RefreshLog ARun(int id, SyncMode mode, DateTime executedAt, List<RefreshFinding>? findings)
            => new() { Id = id, Type = RefreshType.Portfolio, EntityId = 7, Mode = mode, ExecutedAt = executedAt, Findings = findings };

        private static readonly RefreshFinding TheFinding
            = new(RefreshFindingKind.NamedLinkMatchedNothing, RefreshFindingSetting.DependencyField, "depends on", ["is blocked by", "relates to"]);

        private static RefreshFinding AFinding() => TheFinding;

        private static JsonObject SerialisedRowFor(RefreshLog log)
        {
            var dto = RefreshLogDto.ListFor([log]).Single();

            return JsonNode.Parse(JsonSerializer.Serialize(dto, AsTheWireSendsIt))!.AsObject();
        }
    }
}
