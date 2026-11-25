#:project @BACKEND@/Lighthouse.Backend/Lighthouse.Backend.csproj
#:project @BACKEND@/Lighthouse.Migrations.Sqlite/Lighthouse.Migrations.Sqlite.csproj
#:project @BACKEND@/Lighthouse.Migrations.Postgres/Lighthouse.Migrations.Postgres.csproj
#:package Microsoft.AspNetCore.Mvc.Testing@11.0.0
#:property TreatWarningsAsErrors=false
#:property Nullable=enable
#:property PublishAot=false
#:property ValidateExecutableReferencesMatchSelfContained=false

// Shows two things the forecasts do on their own, read through the public API on demo data:
// (1) a Team refresh makes every Portfolio the Team serves forecast again, and
// (2) a restart makes the next forecast compute again, even on data a forecast before the restart found unchanged.
// It starts the checkout's backend in this process on a database of its own, loads one demo scenario and reads
// the refresh history. Arguments: <work-folder> <result-file> [licence-file]
// Writes one line per observation and a last VERDICT line to the result file; exit 0 only when both are observed.
using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;

var workDir = args[0];
var report = new Report(args[1]);
var licence = args.Length > 2 && File.Exists(args[2]) ? args[2] : null;
Directory.CreateDirectory(workDir);

Environment.SetEnvironmentVariable("Database__Provider", "sqlite");
Environment.SetEnvironmentVariable("Database__ConnectionString", $"Data Source={Path.Combine(workDir, "LighthouseAppContext.db")}");
Environment.SetEnvironmentVariable("Encryption__KeyStorePath", Path.Combine(workDir, "keys"));
Environment.SetEnvironmentVariable("Encryption__StartEvenIfNothingStoredCanBeRead", "true");
Environment.SetEnvironmentVariable("Serilog__WriteTo__0__Args__path", Path.Combine(workDir, "logs", "log-.txt"));
Environment.SetEnvironmentVariable("RateLimits__Enabled", "false");
Environment.SetEnvironmentVariable("Lighthouse__TimeZone", "UTC");

const string Rerun = "Rerun with: bash Scripts/examine/bin/forecast-triggers-show (build it first with bash Scripts/examine/build-examine-commands).";

var instance = new Instance();
try
{
    await instance.Start();
    await instance.ImportLicence(licence, report);

    var (scenarioId, scenarioTitle) = await instance.SmallestScenarioWithTeamsAndPortfolios();
    await instance.LoadScenario(scenarioId);
    report.Line($"demo data: scenario {scenarioId} \"{scenarioTitle}\" loaded and every update finished");

    var team = await instance.TeamServingAPortfolio();
    if (team is null)
    {
        report.Verdict(false, $"no Team of scenario {scenarioId} serves a Portfolio, so a Team refresh has nothing to forecast. " +
            "Without such a Team neither observation can be made. Check that the demo data loads its Portfolios; " + Rerun);
        return 2;
    }

    var teamRefreshSeen = await ObserveTheTeamRefresh(team);
    var portfolio = team.PortfolioIds[0];
    var seedingSeen = await ObserveTheStartUpSeeding(portfolio);

    report.Verdict(teamRefreshSeen && seedingSeen,
        teamRefreshSeen && seedingSeen
            ? "both the Team refresh trigger and the start-up forecast seeding were seen through the refresh history"
            : $"{(teamRefreshSeen ? "" : "(1) Team refresh ")}{(seedingSeen ? "" : "(2) start-up seeding ")}not seen; the lines above say what was missing. " + Rerun);
    return teamRefreshSeen && seedingSeen ? 0 : 1;
}
catch (Exception failure)
{
    report.Verdict(false, $"the run stopped: {failure.Message}. Nothing after this point was observed, so neither observation can be trusted. " +
        "Check that the backend starts from the checkout and that the demo data files are present; " + Rerun);
    return 1;
}
finally
{
    await instance.DisposeAsync();
}

async Task<bool> ObserveTheTeamRefresh(TeamServed team)
{
    var before = await instance.ForecastRows();
    var requestedAt = DateTime.UtcNow;
    await instance.Post($"/api/v1/teams/{team.Id}");
    var after = await instance.ForecastRowsOnceThereAreNew(before, requestedAt, team.PortfolioIds);

    var lines = team.PortfolioIds.Select(id =>
    {
        var row = after.Where(r => r.PortfolioId == id && r.ExecutedAt > requestedAt && !before.Any(b => b.Id == r.Id))
            .OrderBy(r => r.ExecutedAt).FirstOrDefault();
        return (id, row);
    }).ToList();
    var missing = lines.Where(l => l.row is null).Select(l => l.id).ToList();
    var found = string.Join(", ", lines.Where(l => l.row is not null).Select(l => $"Portfolio {l.id} at {Stamp(l.row!.ExecutedAt)}"));

    if (missing.Count == 0)
    {
        report.Line($"(1) Team refresh: OBSERVED. Team {team.Id} \"{team.Name}\" serves Portfolios {string.Join(", ", team.PortfolioIds)}; " +
            $"refresh requested at {Stamp(requestedAt)}; a Forecast row followed for every one: {found}");
        return true;
    }

    report.Line($"(1) Team refresh: NOT OBSERVED. Team {team.Id} \"{team.Name}\" serves Portfolios {string.Join(", ", team.PortfolioIds)}; " +
        $"refresh requested at {Stamp(requestedAt)}; no Forecast row after it for Portfolio(s) {string.Join(", ", missing)}" +
        $"{(found.Length > 0 ? $" (rows seen: {found})" : "")}. A Team refresh must make every Portfolio the Team serves forecast " +
        "again, or their dates stay stale until some other refresh. " + Rerun);
    return false;
}

// Switching who owns the Feature order to the value it already has asks every Portfolio to forecast now without
// forgetting what the stored forecasts were computed from, unlike every refresh button. On unchanged data that
// round is confirmed unchanged (Skipped), which leaves the start-up seeding as the only thing that can make the
// first round after a restart compute again.
async Task<bool> ObserveTheStartUpSeeding(int portfolio)
{
    var beforeRound = await instance.ForecastRows();
    var askedBefore = DateTime.UtcNow;
    var answer = await instance.AskEveryPortfolioToForecastOnUnchangedData();
    if (answer is not null)
    {
        report.Line($"(2) start-up seeding: NOT OBSERVED. Asking the Portfolios to forecast on unchanged data was refused: {answer}. " +
            "That round is needed to show the restart makes a difference. The switch is a premium setting; run it in a checkout " +
            "that has the premium licence file the backend tests use. " + Rerun);
        return false;
    }
    var rowsBefore = await instance.ForecastRowsOnceThereAreNew(beforeRound, askedBefore, [portfolio]);
    var roundBefore = rowsBefore.Where(r => r.PortfolioId == portfolio && r.ExecutedAt > askedBefore && !beforeRound.Any(b => b.Id == r.Id))
        .OrderBy(r => r.ExecutedAt).ThenBy(r => r.Id).FirstOrDefault();

    await instance.DisposeAsync();
    instance = new Instance();
    var restartedAt = DateTime.UtcNow;
    await instance.Start();
    await instance.WaitUntilNothingIsUpdating();
    var beforeAfterRound = await instance.ForecastRows();
    var askedAfter = DateTime.UtcNow;
    answer = await instance.AskEveryPortfolioToForecastOnUnchangedData();
    if (answer is not null)
    {
        report.Line($"(2) start-up seeding: NOT OBSERVED. After the restart the request to forecast was refused: {answer}. " + Rerun);
        return false;
    }
    var rowsAfter = await instance.ForecastRowsOnceThereAreNew(beforeAfterRound, askedAfter, [portfolio]);
    var roundAfter = rowsAfter.Where(r => r.PortfolioId == portfolio && r.ExecutedAt > restartedAt)
        .OrderBy(r => r.ExecutedAt).ThenBy(r => r.Id).FirstOrDefault();

    var beforeText = roundBefore is null ? "no Forecast row" : $"Forecast row at {Stamp(roundBefore.ExecutedAt)} Skipped={Flag(roundBefore.Skipped)}";
    var afterText = roundAfter is null ? "no Forecast row" : $"first Forecast row at {Stamp(roundAfter.ExecutedAt)} Skipped={Flag(roundAfter.Skipped)}";
    // A Team or Portfolio refresh between the two rounds could change the data, and a changed forecast computes
    // again with or without the restart, so it would show nothing about the start-up seeding.
    List<string> refreshesBetween = roundBefore is null || roundAfter is null
        ? []
        : await instance.RefreshesBetween(roundBefore.ExecutedAt, roundAfter.ExecutedAt);
    var detail = $"Portfolio {portfolio}: round on unchanged data before the restart: {beforeText}; host restarted at {Stamp(restartedAt)} " +
        $"on the same database; after the restart: {afterText}; Team or Portfolio refreshes between the two rounds: " +
        $"{(refreshesBetween.Count == 0 ? "none" : string.Join(", ", refreshesBetween))}";

    if (roundBefore is { Skipped: true } && roundAfter is { Skipped: false } && refreshesBetween.Count == 0)
    {
        report.Line($"(2) start-up seeding: OBSERVED. {detail}. The forecast found unchanged before the restart was computed again after it, " +
            "because the start-up seeding forgot what the stored forecasts were computed from");
        return true;
    }

    var why = roundBefore is null || roundAfter is null
        ? "a round's Forecast row was missing, so there is nothing to compare"
        : refreshesBetween.Count > 0
            ? "a refresh ran between the two rounds and may have changed the data, so the restart cannot be shown to make the difference"
        : !roundBefore.Skipped
            ? "the round before the restart was not confirmed unchanged, so the restart cannot be shown to make the difference"
            : "the first round after the restart was confirmed unchanged, so nothing made it compute again: a forecast another build " +
              "stored while this instance was down would be kept as if current";
    report.Line($"(2) start-up seeding: NOT OBSERVED. {detail}. {why}. " + Rerun);
    return false;
}

static string Stamp(DateTime instant) => instant.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

static string Flag(bool value) => value ? "true" : "false";

public sealed record TeamServed(int Id, string Name, IReadOnlyList<int> PortfolioIds);

public sealed record ForecastRow(int Id, int PortfolioId, DateTime ExecutedAt, bool Skipped);

public sealed class Report(string path)
{
    public void Line(string text)
    {
        Console.WriteLine(text);
        File.AppendAllText(path, text + "\n");
    }

    public void Verdict(bool observed, string why) =>
        Line($"VERDICT: {(observed ? "OBSERVED" : "NOT OBSERVED")}, {why}");
}

public sealed class Instance : IAsyncDisposable
{
    private WebApplicationFactory<Lighthouse.Backend.Program>? factory;
    private HttpClient? client;

    private HttpClient Client => client ?? throw new InvalidOperationException("the backend was not started");

    public async Task Start()
    {
        factory = new WebApplicationFactory<Lighthouse.Backend.Program>();
        client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        await Get("/api/v1/update/status");
    }

    public async Task ImportLicence(string? licence, Report report)
    {
        if (licence is null)
        {
            report.Line("licence: none, so the premium setting that asks for a forecast on unchanged data is refused");
            return;
        }
        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(await File.ReadAllBytesAsync(licence)), "file", "licence.json");
        var imported = await Client.PostAsync("/api/v1/license/import", form);
        report.Line($"licence: premium licence imported ({(int)imported.StatusCode})");
    }

    public async Task<(int Id, string Title)> SmallestScenarioWithTeamsAndPortfolios()
    {
        var scenarios = (await Get("/api/v1/demo/scenarios")).AsArray()
            .Select(s => s!)
            .Where(s => s["teams"]!.AsArray().Count > 0 && s["projects"]!.AsArray().Count > 0)
            .OrderBy(s => s["teams"]!.AsArray().Count + s["projects"]!.AsArray().Count)
            .ThenBy(s => s["id"]!.GetValue<int>())
            .ToList();
        var chosen = scenarios.FirstOrDefault()
            ?? throw new InvalidOperationException("no demo scenario has both Teams and Portfolios; the demo data files may be missing");
        return (chosen["id"]!.GetValue<int>(), chosen["title"]!.GetValue<string>());
    }

    public async Task LoadScenario(int id)
    {
        var load = await Client.PostAsync($"/api/v1/demo/scenarios/{id}/load", null);
        if (!load.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"loading demo scenario {id} answered {(int)load.StatusCode}");
        }
        await WaitUntilNothingIsUpdating();
    }

    public async Task<TeamServed?> TeamServingAPortfolio()
    {
        var teams = (await Get("/api/v1/teams")).AsArray()
            .Select(t => new TeamServed(
                t!["id"]!.GetValue<int>(),
                t["name"]!.GetValue<string>(),
                t["portfolios"]!.AsArray().Select(p => p!["id"]!.GetValue<int>()).Order().ToList()))
            .Where(t => t.PortfolioIds.Count > 0)
            .OrderBy(t => t.Id)
            .ToList();
        return teams.FirstOrDefault();
    }

    public async Task Post(string path)
    {
        var answer = await Client.PostAsync(path, null);
        if (!answer.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"POST {path} answered {(int)answer.StatusCode}");
        }
    }

    /// <summary>Sends the Feature-order setting back as it is; returns why it was refused, or null.</summary>
    public async Task<string?> AskEveryPortfolioToForecastOnUnchangedData()
    {
        var setting = await Get("/api/v1/optionalfeatures/FeatureOrdering");
        using var body = new StringContent(setting.ToJsonString(), System.Text.Encoding.UTF8, "application/json");
        var answer = await Client.PostAsync("/api/v1/optionalfeatures/FeatureOrdering", body);
        return answer.IsSuccessStatusCode ? null : $"{(int)answer.StatusCode} {await answer.Content.ReadAsStringAsync()}";
    }

    /// <summary>The Team and Portfolio refreshes the history holds strictly between two instants, as "Type id at time".</summary>
    public async Task<List<string>> RefreshesBetween(DateTime from, DateTime to) =>
        [.. (await Get("/api/v1/systeminfo/refreshlog")).AsArray()
            .Select(r => r!)
            .Where(r => !IsForecast(r["type"]!))
            .Select(r => (Type: r["type"]!.ToJsonString().Trim('"'), Id: r["entityId"]!.GetValue<int>(), At: Instant(r["executedAt"]!)))
            .Where(r => r.At > from && r.At < to)
            .OrderBy(r => r.At)
            .Select(r => $"{r.Type} {r.Id} at {r.At.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture)}")];

    private static DateTime Instant(JsonNode value) =>
        DateTime.Parse(value.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    public async Task<List<ForecastRow>> ForecastRows() =>
        [.. (await Get("/api/v1/systeminfo/refreshlog")).AsArray()
            .Select(r => r!)
            .Where(r => IsForecast(r["type"]!))
            .Select(r => new ForecastRow(
                r["id"]!.GetValue<int>(),
                r["entityId"]!.GetValue<int>(),
                DateTime.Parse(r["executedAt"]!.GetValue<string>(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                r["skipped"]!.GetValue<bool>()))];

    /// <summary>
    /// Waits for the updates to finish, then for a new Forecast row per Portfolio for a short while, because a
    /// forecast held for its Teams joins the queue only once they are done. Returns what the history holds then.
    /// </summary>
    public async Task<List<ForecastRow>> ForecastRowsOnceThereAreNew(List<ForecastRow> before, DateTime since, IReadOnlyList<int> portfolioIds)
    {
        await WaitUntilNothingIsUpdating();
        var rows = await ForecastRows();
        for (var i = 0; i < 20 && !portfolioIds.All(id => rows.Any(r => r.PortfolioId == id && r.ExecutedAt > since && before.All(b => b.Id != r.Id))); i++)
        {
            await Task.Delay(1000);
            await WaitUntilNothingIsUpdating();
            rows = await ForecastRows();
        }
        return rows;
    }

    public async Task WaitUntilNothingIsUpdating()
    {
        var quiet = 0;
        for (var i = 0; i < 600 && quiet < 3; i++)
        {
            await Task.Delay(300);
            var status = await Get("/api/v1/update/status");
            quiet = status["hasActiveUpdates"]!.GetValue<bool>() ? 0 : quiet + 1;
        }
        if (quiet < 3)
        {
            throw new TimeoutException("updates were still running after 3 minutes");
        }
    }

    public async ValueTask DisposeAsync()
    {
        client?.Dispose();
        client = null;
        if (factory is not null)
        {
            await factory.DisposeAsync();
            factory = null;
        }
    }

    private static bool IsForecast(JsonNode type) =>
        type.GetValueKind() == System.Text.Json.JsonValueKind.String
            ? type.GetValue<string>() == "Forecast"
            : type.GetValue<int>() == (int)Lighthouse.Backend.Models.RefreshType.Forecast;

    private async Task<JsonNode> Get(string path) =>
        JsonNode.Parse(await Client.GetStringAsync(path)) ?? throw new InvalidOperationException($"empty answer from {path}");
}
