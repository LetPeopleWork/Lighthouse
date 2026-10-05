using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// How the refinement-need scenarios reach the system: the same real host and database as every other
    /// Refinement scenario, through the Team settings write and read, the vote write, the Refinement tab's
    /// read and the manual forecast. Stages, the Refinement cadence and the band are only ever put there by
    /// the admin's own save.
    ///
    /// Two things are stood in for. The instance's clock, so a scenario can say which day it is - in the
    /// instance's own time zone. And, per horizon, how many Work Items the Team is likely to pull: a scenario
    /// about a verdict has to choose the forecast it is judged against, or it asserts sampling noise. A
    /// horizon the scenario did not script runs the shipped forecast engine over the Team's real finished
    /// Work Items - which is also how the scenarios prove that the tab asks the forecast for exactly the
    /// working days until the next Refinement: a scripted range only appears when the tab asks for the
    /// horizon it was scripted for.
    /// </summary>
    public abstract class RefinementNeedAcceptanceTest : SizingVotesAcceptanceTest
    {
        protected const string PendingSlice04 = "Epic #5881 slice 04 (#6142) - pending DELIVER";

        protected const string PendingSlice05 = "Epic #5881 slice 05 (#6143) - pending DELIVER";

        protected const string PendingSlice07 = "Epic #5881 slice 07 (#6145) - pending DELIVER";

        protected const string PendingSlice09 = "Epic #5881 slice 09 (#6147) - pending DELIVER";

        protected const string AdvancedSearch = "GR-059";

        // --- Stages ---

        protected const string Waiting = "Waiting";

        protected const string BeingRefined = "BeingRefined";

        protected const string ReadyStage = "Ready";

        /// <summary>The ready count is what the stage rules say.</summary>
        protected const string FromStages = "Stages";

        /// <summary>The ready count is what the votes say, as on a Team that sets no stage rule.</summary>
        protected const string FromVotes = "Votes";

        protected const string ReadyTag = "ready";

        protected const string AnalysingTag = "analysing";

        private const string TagsField = "workitem.tags";

        private const string ContainsOperator = "contains";

        // --- Verdicts and why there is none ---

        protected const string Below = "Below";

        protected const string InRange = "In";

        protected const string Above = "Above";

        protected const string NoCadence = "NoCadence";

        protected const string InsufficientData = "InsufficientData";

        protected const string NoRefinementStates = "NoRefinementStates";

        protected const string Monday = "Monday";

        protected const string Tuesday = "Tuesday";

        protected const string Thursday = "Thursday";

        private const string RefinementMember = "refinement";

        private static readonly JsonElement NoSection = AnEmptyObject();

        private readonly NeedForecastScript forecasts = new();

        // NUnit runs every scenario of a fixture on one instance, so a forecast one scenario scripted would
        // otherwise answer the next scenario that asks about the same number of working days.
        [SetUp]
        public void ForgetTheScriptedForecasts() => forecasts.Forget();

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            var script = forecasts;

            services.RemoveAll<IForecastService>();
            services.AddScoped<ForecastService>();
            services.AddScoped<IForecastService>(provider =>
                new ForecastWithScriptedHorizons(provider.GetRequiredService<ForecastService>(), script));
        }

        // --- Which day it is ---

        /// <summary>The instance believes it is this day, nine in the morning, in a time zone that is UTC.</summary>
        protected void TodayIs(int year, int month, int day)
            => InstanceClock.SetInstant(new DateTimeOffset(year, month, day, 9, 0, 0, TimeSpan.Zero));

        /// <summary>The instance believes it is this instant, and reads calendar days in this time zone.</summary>
        protected void TheInstantIs(DateTimeOffset instant, string timeZoneId)
        {
            InstanceClock.SetZone(TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
            InstanceClock.SetInstant(instant);
        }

        // --- Preconditions ---

        /// <summary>
        /// Team Gravity refines in Backlog, Analysing and Next, and its tracker holds, in backlog order: GR-040
        /// being implemented (not in refinement), GR-058 and GR-059 in Next, GR-051 and GR-054 in Analysing,
        /// GR-073 and GR-074 in Backlog - six Work Items in refinement. The given Work Items carry the given
        /// tags. Nobody has voted, and no stage, cadence or band has been chosen.
        /// </summary>
        protected async Task<TeamUnderTest> GravityRefinesSixWorkItems(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = SeedTeam("Team Gravity", [Backlog], [Next, Analysing, Implementation], [Done], [], 85, 7);

            SeedTaggedWorkItems(gravity, tags,
            [
                new TrackerWorkItem(BillingExport, "Billing export", Implementation, StateCategories.Doing, "1", StartedDaysAgo: 9),
                new TrackerWorkItem(UserActivityTracking, "User activity tracking", Next, StateCategories.Doing, "2", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedSearch, "Advanced search filters", Next, StateCategories.Doing, "3", StartedDaysAgo: 1),
                new TrackerWorkItem(AdvancedReporting, "Advanced reporting module", Analysing, StateCategories.Doing, "4", StartedDaysAgo: 3),
                new TrackerWorkItem(ApiVersioning, "Public API versioning", Analysing, StateCategories.Doing, "5", StartedDaysAgo: 2),
                new TrackerWorkItem(ConfigurationManagement, "Configuration management", Backlog, StateCategories.ToDo, "6"),
                new TrackerWorkItem(LoadTesting, "Load testing framework", Backlog, StateCategories.ToDo, "7"),
            ]);

            return await TheAdminHasChosen(gravity, Backlog, Analysing, Next);
        }

        /// <summary>Work Items the tracker holds, the ones named carrying the given tag.</summary>
        protected void SeedTaggedWorkItems(TeamUnderTest team, (string WorkItem, string Tag)[] tags, List<TrackerWorkItem> workItems)
        {
            using var scope = Factory.Services.CreateScope();

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var owner = teamRepository.GetById(team.TeamId) ?? throw new InvalidOperationException($"Team {team.TeamId} not found");
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();

            foreach (var tracked in workItems)
            {
                var started = tracked.Category == StateCategories.Doing
                    ? Today.UtcDateTime.Date.AddDays(-tracked.StartedDaysAgo).AddHours(12)
                    : (DateTime?)null;

                workItemRepository.Add(new WorkItem
                {
                    Team = owner,
                    TeamId = owner.Id,
                    ReferenceId = tracked.ReferenceId,
                    Name = tracked.Name,
                    Type = "User Story",
                    State = tracked.State,
                    StateCategory = tracked.Category,
                    CreatedDate = Today.UtcDateTime.AddDays(-60),
                    StartedDate = started,
                    Order = tracked.Rank,
                    Url = TrackerAddressOf(tracked.ReferenceId),
                    ParentReferenceId = tracked.ParentReferenceId,
                    Tags = [.. tags.Where(tag => tag.WorkItem == tracked.ReferenceId).Select(tag => tag.Tag)],
                });
            }

            workItemRepository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// The Team finished this many Work Items on each of the last <paramref name="days"/> days, today
        /// included, each taking one day. A Team that finishes the same number every day makes the shipped
        /// forecast engine answer one number, whatever it draws.
        /// </summary>
        protected void TheTeamFinishedWorkEveryDay(TeamUnderTest team, int perDay, int days = ThroughputHistoryDays)
        {
            var finished = Enumerable.Range(0, days)
                .SelectMany(daysAgo => Enumerable.Range(0, perDay)
                    .Select(index => new FinishedWorkItem($"DONE-{daysAgo}-{index}", 1, daysAgo)))
                .ToArray();

            SeedFinishedWorkItems(team, finished);
        }

        /// <summary>
        /// Over this many working days the Team is likely to pull the given number of Work Items at each given
        /// likelihood, read the way the band reads them: "at 85%" is a count only 15% of runs pull more than.
        /// Between two given likelihoods the higher one's count holds.
        /// </summary>
        protected void TheTeamIsLikelyToPull(int overWorkingDays, params (int Percentile, int Count)[] likelihoods)
            => forecasts.Script(overWorkingDays, likelihoods);

        protected async Task<TeamUnderTest> GravityWithItsReadyRule(params (string WorkItem, string Tag)[] tags)
        {
            var gravity = await GravityRefinesSixWorkItems(tags);
            await TheAdminHasSetTheStageRules(gravity, ready: TagsContain(ReadyTag), beingRefined: null);
            return gravity;
        }

        /// <summary>
        /// Gravity's GR-058 and GR-059 are tagged ready under a Ready rule, it refines on Thursdays every week,
        /// and today is Friday 2 October 2026 - six days before Thursday 8 October. It finished one Work Item a
        /// day over its Throughput window, which is enough history for a forecast.
        /// </summary>
        protected async Task<TeamUnderTest> GravityWithTwoReadyRefiningOnThursdaysOnFridayTheSecond()
        {
            var gravity = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            await TheAdminHasSetTheCadence(gravity, [Thursday], 1, null);
            TodayIs(2026, 10, 2);
            return gravity;
        }

        /// <summary>
        /// Over the six working days to Thursday the Team is likely to pull a different number at every
        /// likelihood: 3 at 30%, 5 at the median, 6 at 70%, 8 at 85% and 10 at 95%.
        /// </summary>
        protected void TheTeamIsLikelyToPullFiveToEightBeforeThursday()
            => TheTeamIsLikelyToPull(6, (30, 3), (50, 5), (70, 6), (85, 8), (95, 10));

        protected async Task<TeamUnderTest> GravityWithTwoReadyLikelyToPullFiveToEightBeforeThursday()
        {
            var gravity = await GravityWithTwoReadyRefiningOnThursdaysOnFridayTheSecond();
            TheTeamIsLikelyToPullFiveToEightBeforeThursday();
            return gravity;
        }

        /// <summary>A system admin has declared this day a blackout day for the whole instance.</summary>
        protected async Task ABlackoutDayOn(DateOnly day)
        {
            TheCallerAdministersTheWholeInstance();

            var body = new JsonObject
            {
                ["start"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["end"] = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["description"] = "Company offsite",
            };

            using var created = await Client.PostAsync(
                "/api/latest/blackout-periods",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
            Assert.That(created.IsSuccessStatusCode, Is.True,
                $"The blackout day was not declared, so the scenario describes a calendar without it. {await created.Content.ReadAsStringAsync()}");
        }

        // --- The admin's saves ---

        protected static JsonObject TagsContain(string tag) => new()
        {
            ["version"] = 1,
            ["mode"] = "and",
            ["conditions"] = new JsonArray(new JsonObject
            {
                ["fieldKey"] = TagsField,
                ["operator"] = ContainsOperator,
                ["value"] = tag,
            }),
        };

        protected static JsonObject ARuleOn(string fieldKey, string value) => ARuleOn(fieldKey, ContainsOperator, value);

        protected static JsonObject ARuleOn(string fieldKey, string ruleOperator, string value) => new()
        {
            ["version"] = 1,
            ["mode"] = "and",
            ["conditions"] = new JsonArray(new JsonObject
            {
                ["fieldKey"] = fieldKey,
                ["operator"] = ruleOperator,
                ["value"] = value,
            }),
        };

        /// <summary>How <see cref="TagsContain"/> reads back, or null for no rule.</summary>
        protected static StageRuleReading? TagsContainReading(string? tag)
            => tag is null ? null : new StageRuleReading("and", 1, TagsField, ContainsOperator, tag);

        /// <summary>A rule of one condition whose operator takes no value, sent with an empty value as the rule editor sends it.</summary>
        protected static JsonObject ARuleWithoutAValue(string fieldKey, string valuelessOperator) => ARuleOn(fieldKey, valuelessOperator, "");

        /// <summary>A rule with this many conditions, each "Tags contains" a different word.</summary>
        protected static JsonObject ARuleWithConditions(int count) => ARuleWithConditionsOn(TagsField, count);

        /// <summary>A rule with this many conditions on the field, each "contains" a different word.</summary>
        protected static JsonObject ARuleWithConditionsOn(string fieldKey, int count) => new()
        {
            ["version"] = 1,
            ["mode"] = "or",
            ["conditions"] = new JsonArray([.. Enumerable.Range(1, count).Select(index => (JsonNode)new JsonObject
            {
                ["fieldKey"] = fieldKey,
                ["operator"] = ContainsOperator,
                ["value"] = $"tag-{index}",
            })]),
        };

        protected static JsonObject StageRules(JsonObject? ready, JsonObject? beingRefined) => new()
        {
            ["ready"] = ready,
            ["beingRefined"] = beingRefined,
        };

        protected async Task TheAdminHasSetTheStageRules(TeamUnderTest team, JsonObject? ready, JsonObject? beingRefined)
        {
            using var save = await SaveTheRefinementSectionWith(team, "stageRules", StageRules(ready, beingRefined));
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's stage rules were not saved, so nothing that follows can build on them. {await save.Content.ReadAsStringAsync()}");
        }

        protected static JsonObject ACadence(string[] weekdays, int intervalWeeks, string? anchorWeek)
        {
            var cadence = new JsonObject
            {
                ["weekdays"] = new JsonArray([.. weekdays.Select(day => (JsonNode)JsonValue.Create(day))]),
                ["intervalWeeks"] = intervalWeeks,
            };

            if (anchorWeek is not null)
            {
                cadence["anchorWeek"] = anchorWeek;
            }

            return cadence;
        }

        protected async Task TheAdminHasSetTheCadence(TeamUnderTest team, string[] weekdays, int intervalWeeks, string? anchorWeek)
        {
            using var save = await SaveTheRefinementSectionWith(team, "cadence", ACadence(weekdays, intervalWeeks, anchorWeek));
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's Refinement cadence was not saved, so nothing that follows can build on it. {await save.Content.ReadAsStringAsync()}");
        }

        /// <summary>The band section with the percentiles given; one left null is left out of the save.</summary>
        protected static JsonObject ABand(int? lowPercentile, int? highPercentile)
        {
            var band = new JsonObject();
            if (lowPercentile is not null)
            {
                band["lowPercentile"] = lowPercentile;
            }

            if (highPercentile is not null)
            {
                band["highPercentile"] = highPercentile;
            }

            return band;
        }

        protected async Task TheAdminHasSetTheBand(TeamUnderTest team, int lowPercentile, int highPercentile)
        {
            using var save = await SaveTheRefinementSectionWith(team, "band", ABand(lowPercentile, highPercentile));
            Assert.That(save.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"The admin's band was not saved, so nothing that follows can build on it. {await save.Content.ReadAsStringAsync()}");
        }

        /// <summary>
        /// The settings form saved by a Team admin with the Team's current refinement states and one more
        /// member of the refinement section - or none, when <paramref name="member"/> is null.
        /// </summary>
        protected async Task<HttpResponseMessage> SaveTheRefinementSectionWith(TeamUnderTest team, string? member, JsonNode? value)
        {
            TheCallerAdministersTheTeam(team);

            var settings = await ReadTheTeamSettings(team);
            var states = RefinementStatesIn(settings).Select(state => (JsonNode)new JsonObject { ["state"] = state.State }).ToArray();

            var refinement = new JsonObject { ["states"] = new JsonArray(states) };
            if (member is not null)
            {
                refinement[member] = value;
            }

            var payload = TheTeamSettingsFormFor(team);
            payload[RefinementMember] = refinement;

            return await PutTheTeamSettings(team, payload);
        }

        /// <summary>The settings form saved as an older form would, saying nothing about the member a scenario is about.</summary>
        protected async Task<HttpResponseMessage> SaveTheSettingsShaped(TeamUnderTest team, SaveShape shape)
            => shape == SaveShape.WithoutTheRefinementSection
                ? await SaveTheTeamSettingsLeavingRefinementOut(team)
                : await SaveTheRefinementSectionWith(team, null, null);

        // --- Other driving ports ---

        /// <summary>The Team's manual How Many forecast for this target date, as the Forecasts tab asks for it.</summary>
        protected async Task<JsonElement> TheManualForecastFor(TeamUnderTest team, DateOnly targetDate)
        {
            var body = new JsonObject
            {
                ["targetDate"] = targetDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "T00:00:00Z",
            };

            using var response = await Client.PostAsync(
                $"/api/latest/forecast/manual/{team.TeamId}",
                new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"));
            var answer = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"The manual forecast answered {(int)response.StatusCode}: {answer}");

            using var document = JsonDocument.Parse(answer);
            return document.RootElement.Clone();
        }

        protected static int? HowManyAt(JsonElement manualForecast, int probability)
        {
            if (!manualForecast.TryGetProperty("howManyForecasts", out var forecasts) || forecasts.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            return forecasts.EnumerateArray()
                .Where(level => NumberOf(level, "probability") == probability)
                .Select(level => NumberOf(level, "value"))
                .FirstOrDefault();
        }

        // --- Reading the answers ---

        /// <summary>One row of the tab as far as stages go. Absent facts read as null.</summary>
        protected static StageRowReading StageRowOf(JsonElement tab, string referenceId)
        {
            var votes = RowOf(tab, referenceId);
            var row = tab.GetProperty("workItems").EnumerateArray().First(candidate => TextOf(candidate, "referenceId") == referenceId);

            return new StageRowReading(referenceId, TextOf(row, "stage"), votes.Readiness, votes.MissingVotes, FlagOf(row, "signalsDisagree"));
        }

        protected static int? ReadyCountIn(JsonElement tab) => NumberOf(tab, "readyCount");

        /// <summary>Which signal the ready count follows: the votes, or - once the Team sets stage rules - the stages.</summary>
        protected static string? ReadySourceIn(JsonElement tab) => TextOf(tab, "readySource");

        protected static bool? StagesConfiguredIn(JsonElement tab) => FlagOf(tab, "stagesConfigured");

        protected static CadenceFactsReading CadenceFactsIn(JsonElement tab)
            => new(TextOf(tab, "nextRefinementDate"), FlagOf(tab, "isRefinementDay"));

        protected static NeedReading NeedIn(JsonElement tab)
        {
            Assert.That(tab.TryGetProperty("need", out var need) && need.ValueKind == JsonValueKind.Object, Is.True,
                $"The Refinement tab's answer says nothing about how many Work Items are needed. Body: {tab}");

            return new NeedReading(
                TextOf(need, "verdict"),
                TextOf(need, "unavailableReason"),
                NumberOf(need, "low"),
                NumberOf(need, "high"),
                NumberOf(need, "lowPercentile"),
                NumberOf(need, "highPercentile"),
                NumberOf(need, "horizonWorkingDays"));
        }

        protected static List<string> ReferencesListedIn(JsonElement tab)
            => [.. RowsIn(tab).Select(row => row.ReferenceId ?? string.Empty)];

        /// <summary>The refinement section of a settings answer; a Team never configured reads as an empty object.</summary>
        protected static JsonElement RefinementSectionOf(JsonElement settings)
            => settings.TryGetProperty(RefinementMember, out var refinement) && refinement.ValueKind == JsonValueKind.Object
                ? refinement
                : NoSection;

        protected static CadenceReading? CadenceIn(JsonElement settings)
        {
            var refinement = RefinementSectionOf(settings);
            if (!refinement.TryGetProperty("cadence", out var cadence) || cadence.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var weekdays = cadence.TryGetProperty("weekdays", out var days) && days.ValueKind == JsonValueKind.Array
                ? string.Join(",", days.EnumerateArray().Select(day => day.GetString()))
                : null;

            return new CadenceReading(weekdays, NumberOf(cadence, "intervalWeeks"), TextOf(cadence, "anchorWeek"));
        }

        protected static BandReading BandIn(JsonElement settings)
        {
            var refinement = RefinementSectionOf(settings);
            Assert.That(refinement.TryGetProperty("band", out var band) && band.ValueKind == JsonValueKind.Object, Is.True,
                $"The Team's settings carry no band. Body: {settings}");

            return new BandReading(NumberOf(band, "lowPercentile"), NumberOf(band, "highPercentile"));
        }

        /// <summary>Each stage rule as the settings read gives it back, or null when that stage has no rule.</summary>
        protected static StageRulesReading StageRulesIn(JsonElement settings)
        {
            var refinement = RefinementSectionOf(settings);
            if (!refinement.TryGetProperty("stageRules", out var rules) || rules.ValueKind != JsonValueKind.Object)
            {
                return new StageRulesReading(null, null);
            }

            return new StageRulesReading(StageRuleOf(rules, "ready"), StageRuleOf(rules, "beingRefined"));
        }

        private static JsonElement AnEmptyObject()
        {
            using var document = JsonDocument.Parse("{}");
            return document.RootElement.Clone();
        }

        private static StageRuleReading? StageRuleOf(JsonElement rules, string stage)
        {
            if (!rules.TryGetProperty(stage, out var rule) || rule.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            List<JsonElement> conditions = rule.TryGetProperty("conditions", out var array) && array.ValueKind == JsonValueKind.Array
                ? [.. array.EnumerateArray()]
                : [];
            string? OfFirst(string property) => conditions.Count == 0 ? null : TextOf(conditions[0], property);

            return new StageRuleReading(TextOf(rule, "mode"), conditions.Count, OfFirst("fieldKey"), OfFirst("operator"), OfFirst("value"));
        }

        protected int SizingEntriesStoredFor(TeamUnderTest team)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            return context.SizingLogEntries.Count(entry => entry.TeamId == team.TeamId);
        }

        // --- Domain types ---

        /// <summary>How a save that says nothing about a refinement member is shaped.</summary>
        public enum SaveShape
        {
            WithoutTheRefinementSection,
            RefinementSectionWithoutTheMember,
        }

        /// <summary>
        /// One row's two signals: the stage its rules give it (null when the Team sets no rule), what its votes
        /// say, and whether the two disagree.
        /// </summary>
        protected sealed record StageRowReading(string ReferenceId, string? Stage, string? VotesSay, int? MissingVotes, bool? SignalsDisagree);

        protected sealed record CadenceFactsReading(string? NextRefinementDate, bool? IsRefinementDay);

        protected sealed record CadenceReading(string? Weekdays, int? IntervalWeeks, string? AnchorWeek);

        protected sealed record BandReading(int? LowPercentile, int? HighPercentile);

        protected sealed record StageRulesReading(StageRuleReading? Ready, StageRuleReading? BeingRefined);

        /// <summary>A stage rule's mode, how many conditions it has, and the field, operator and value of its first.</summary>
        protected sealed record StageRuleReading(string? Mode, int ConditionCount, string? FieldKey, string? Operator, string? Value);

        /// <summary>
        /// The need as facts: the verdict or why there is none, the range and its likelihoods, and the working
        /// days it covers. Where the "enough for" line falls is the browser's to work out from the high end,
        /// because it follows whatever order the rows are shown in.
        /// </summary>
        protected sealed record NeedReading(
            string? Verdict,
            string? UnavailableReason,
            int? Low,
            int? High,
            int? LowPercentile,
            int? HighPercentile,
            int? HorizonWorkingDays);

        /// <summary>
        /// The forecasts a scenario chose, by the number of working days they cover. A simulation of one hundred
        /// runs is built so that reading it at (100 - p) gives exactly the count chosen for likelihood p.
        /// </summary>
        private sealed class NeedForecastScript
        {
            private const int Runs = 100;

            private readonly Dictionary<int, List<(int Percentile, int Count)>> byHorizon = [];

            private readonly Lock scripting = new();

            public void Script(int workingDays, (int Percentile, int Count)[] likelihoods)
            {
                lock (scripting)
                {
                    byHorizon[workingDays] = [.. likelihoods.OrderBy(likelihood => likelihood.Percentile)];
                }
            }

            public void Forget()
            {
                lock (scripting)
                {
                    byHorizon.Clear();
                }
            }

            public HowManyForecast? ForecastFor(int days)
            {
                List<(int Percentile, int Count)>? likelihoods;
                lock (scripting)
                {
                    if (!byHorizon.TryGetValue(days, out likelihoods))
                    {
                        return null;
                    }
                }

                var runs = new Dictionary<int, int>();
                for (var position = 1; position <= Runs; position++)
                {
                    var count = CountAt(likelihoods, Runs - position);
                    runs[count] = runs.GetValueOrDefault(count) + 1;
                }

                return new HowManyForecast(runs, days);
            }

            private static int CountAt(List<(int Percentile, int Count)> likelihoods, int percentile)
                => likelihoods.Where(likelihood => likelihood.Percentile >= percentile).Select(likelihood => likelihood.Count).DefaultIfEmpty(likelihoods[^1].Count).First();
        }

        /// <summary>The shipped forecast engine, except that a horizon the scenario scripted gets the scripted forecast.</summary>
        private sealed class ForecastWithScriptedHorizons(ForecastService shipped, NeedForecastScript script) : IForecastService
        {
            public Task UpdateForecastsForPortfolio(Portfolio portfolio) => shipped.UpdateForecastsForPortfolio(portfolio);

            public HowManyForecast HowMany(RunChartData throughput, int days)
                => script.ForecastFor(days) ?? shipped.HowMany(throughput, days);

            public HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast)
                => shipped.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            public Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
                => shipped.When(team, remainingItems, mode);
        }
    }
}
