using System.Globalization;
using System.Net;
using System.Text.Json;
using static Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck.RealityCheckAnswer;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// The check's answer as a caller receives it. Every field is looked up by its wire name and a
    /// missing one fails as an assertion naming the field, so a partly built answer reads as a partly
    /// built answer.
    /// </summary>
    public sealed class RealityCheckAnswer(JsonElement root)
    {
        public JsonElement Root { get; } = root;

        public DateOnly AnchorDate => AsDay(Field(Root, "anchorDate"));

        public List<int> StandardWindowDays => Numbers(Field(Root, "standardWindowDays"));

        public List<int> SampledWindowDays => Numbers(Field(Root, "sampledWindowDays"));

        public List<int> SampledHorizonDays => Numbers(Field(Root, "sampledHorizonDays"));

        public List<int> ConfidenceLevels => Numbers(Field(Root, "confidenceLevels"));

        public int MinimumActiveDays => Field(Root, "minimumActiveDays").GetInt32();

        public int RunsAttempted => Field(Field(Root, "denominator"), "runsAttempted").GetInt32();

        public int RunsEvaluated => Field(Field(Root, "denominator"), "runsEvaluated").GetInt32();

        public int LevelsPerRun => Field(Field(Root, "denominator"), "levelsPerRun").GetInt32();

        public int ScoresEvaluated => Field(Field(Root, "denominator"), "scoresEvaluated").GetInt32();

        public List<int> SoundWindowDays => Numbers(Field(Field(Root, "soundWindow"), "soundWindowDays"));

        public List<int> UnevaluatedWindowDays => Numbers(Field(Field(Root, "soundWindow"), "unevaluatedWindowDays"));

        public string Determination => Text(Field(Field(Root, "soundWindow"), "determination"));

        public string? CurrentSettingNotTestedReason
        {
            get
            {
                var reason = Field(Field(Root, "soundWindow"), "currentSettingNotTestedReason");
                return reason.ValueKind == JsonValueKind.Null ? null : Text(reason);
            }
        }

        public int CurrentSettingDays => Field(Field(Root, "soundWindow"), "currentSettingDays").GetInt32();

        public bool CurrentSettingWasTested => Field(Field(Root, "soundWindow"), "currentSettingWasTested").GetBoolean();

        public string CurrentSettingStanding => Text(Field(Field(Root, "soundWindow"), "currentSettingStanding"));

        public List<CellReading> Cells =>
            [.. Field(Root, "cells").EnumerateArray().Select(CellReading.From)];

        /// <summary>
        /// The periods the checks were scored on, one per horizon, as the answer lists them. An answer without
        /// them fails on the field's name and then reads as listing none, so every later assertion fails as an
        /// assertion rather than on reading a value that is not there.
        /// </summary>
        public List<ScoredPeriodReading> ScoredPeriods
        {
            get
            {
                var periods = Field(Root, "scoredPeriods");
                return periods.ValueKind == JsonValueKind.Array
                    ? [.. periods.EnumerateArray().Select(ScoredPeriodReading.From)]
                    : [];
            }
        }

        /// <summary>
        /// Reads the answer an HTTP caller received, so a check that did not answer fails on this assertion
        /// rather than on a JSON parser choking on an empty body. It is also where every check is held to
        /// learning from exactly its own window: no scenario here has a blackout day, so a history one day
        /// longer or shorter than a swept window means a check read days that were not its own.
        /// </summary>
        public static async Task<RealityCheckAnswer> ReadFrom(HttpResponseMessage response, RealityCheckForecastScript forecasts)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    $"The reality check did not answer. Body: {body}");
                Assert.That(forecasts.HistoriesNoSweptWindowIsAsLongAs, Is.Empty,
                    "every check learns from exactly as many days as its window holds; these history lengths match no window swept");
            }

            using var document = JsonDocument.Parse(body);
            return new RealityCheckAnswer(document.RootElement.Clone());
        }

        public static JsonElement Field(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value;
            }

            Assert.Fail($"The answer carries no '{name}' where the contract puts one: {element}");
            return default;
        }

        public CellReading Cell(int samplingWindowDays, int horizonDays)
        {
            var matching = Cells
                .Where(cell => cell.SamplingWindowDays == samplingWindowDays && cell.HorizonDays == horizonDays)
                .ToList();

            Assert.That(matching, Has.Count.EqualTo(1),
                $"expected exactly one check for the {samplingWindowDays}-day window over {horizonDays} days");

            return matching[0];
        }

        public List<CellReading> CellsFor(int samplingWindowDays)
            => [.. Cells.Where(cell => cell.SamplingWindowDays == samplingWindowDays)];

        public LevelCoverageReading Coverage(int confidenceLevel)
        {
            var matching = Field(Root, "levelCoverage").EnumerateArray()
                .Select(LevelCoverageReading.From)
                .Where(coverage => coverage.ConfidenceLevel == confidenceLevel)
                .ToList();

            Assert.That(matching, Has.Count.EqualTo(1),
                $"expected exactly one line for the {confidenceLevel}% level");

            return matching[0];
        }

        internal static string Text(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetInt32().ToString(CultureInfo.InvariantCulture),
            _ => element.ToString(),
        };

        internal static DateOnly AsDay(JsonElement element)
            => DateOnly.ParseExact(element.GetString() ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        private static List<int> Numbers(JsonElement array)
            => array.ValueKind == JsonValueKind.Array ? [.. array.EnumerateArray().Select(number => number.GetInt32())] : [];
    }

    public sealed record CellReading(
        int HorizonDays,
        int SamplingWindowDays,
        DateOnly ScoredPeriodStart,
        DateOnly ScoredPeriodEnd,
        DateOnly HistoryWindowStart,
        DateOnly HistoryWindowEnd,
        bool IsSufficient,
        string Reason,
        int DaysWithCompletedWork,
        Dictionary<int, int> ForecastByLevel,
        int? ActualCompleted,
        string? Outcome,
        Dictionary<int, bool> HeldByLevel)
    {
        public static CellReading From(JsonElement cell)
        {
            var sufficiency = Field(cell, "sufficiency");
            var forecast = Field(cell, "forecast");
            var levelOutcomes = Field(cell, "levelOutcomes");
            var actual = Field(cell, "actualCompleted");
            var outcome = Field(cell, "outcome");

            return new CellReading(
                Field(cell, "horizonDays").GetInt32(),
                Field(cell, "samplingWindowDays").GetInt32(),
                AsDay(Field(cell, "scoredPeriodStart")),
                AsDay(Field(cell, "scoredPeriodEnd")),
                AsDay(Field(cell, "historyWindowStart")),
                AsDay(Field(cell, "historyWindowEnd")),
                Field(sufficiency, "isSufficient").GetBoolean(),
                Text(Field(sufficiency, "reason")),
                Field(sufficiency, "daysWithCompletedWork").GetInt32(),
                forecast.ValueKind == JsonValueKind.Array
                    ? forecast.EnumerateArray().ToDictionary(
                        level => Field(level, "probability").GetInt32(),
                        level => Field(level, "value").GetInt32())
                    : [],
                actual.ValueKind == JsonValueKind.Number ? actual.GetInt32() : null,
                outcome.ValueKind == JsonValueKind.String ? outcome.GetString() : null,
                levelOutcomes.ValueKind == JsonValueKind.Array
                    ? levelOutcomes.EnumerateArray().ToDictionary(
                        level => Field(level, "confidenceLevel").GetInt32(),
                        level => Field(level, "held").GetBoolean())
                    : []);
        }
    }

    /// <summary>
    /// One period a check was scored on, and what the Team finished in it. The actual is read as nullable so
    /// that an answer leaving it out reads as a missing actual rather than as zero.
    /// </summary>
    public sealed record ScoredPeriodReading(int HorizonDays, DateOnly ScoredPeriodStart, DateOnly ScoredPeriodEnd, int? ActualCompleted)
    {
        public static ScoredPeriodReading From(JsonElement period)
        {
            var actual = Field(period, "actualCompleted");

            return new ScoredPeriodReading(
                Field(period, "horizonDays").GetInt32(),
                AsDay(Field(period, "scoredPeriodStart")),
                AsDay(Field(period, "scoredPeriodEnd")),
                actual.ValueKind == JsonValueKind.Number ? actual.GetInt32() : null);
        }
    }

    public sealed record LevelCoverageReading(int ConfidenceLevel, int HeldCount, double ExpectedHeldCount, string Reading)
    {
        public static LevelCoverageReading From(JsonElement line) => new(
            Field(line, "confidenceLevel").GetInt32(),
            Field(line, "heldCount").GetInt32(),
            Field(line, "expectedHeldCount").GetDouble(),
            Text(Field(line, "reading")));
    }
}
