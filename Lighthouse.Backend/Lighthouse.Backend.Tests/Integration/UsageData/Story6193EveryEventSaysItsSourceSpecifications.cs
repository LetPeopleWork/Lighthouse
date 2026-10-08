using System.Text.Json;
using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// Step definitions for story 6193 on the Lighthouse side. Everything is read off what crosses the
    /// instance's edges: the status a caller is answered with, the state body it is served, the messages
    /// the recorder saw leave for the collector, and the usage data page.
    /// </summary>
    public partial class Story6193EveryEventSaysItsSourceTest : UsageDataCollectorObservationTest
    {
        private const string Browser = "Browser";
        private const string Cli = "Cli";
        private const string Mcp = "Mcp";

        private const string ForecastRun = "TeamManualForecastRun";
        private const string TeamRefreshed = "TeamRefreshTriggered";
        private const string PortfolioRefreshed = "PortfolioRefreshTriggered";

        private const string SourceLeftOut = "left out";
        private const string SourceSentAsNull = "null";

        private const string WithNoToken = "no token";
        private const string WithATokenNeverMintedHere = "a token never minted here";
        private const string WithAWithdrawnToken = "a withdrawn token";
        private const string WithAGrantedToken = "a granted token";

        private const string BatchLhSendsForAVoteThatMadeReady = "batch-lh-sends-for-a-vote-that-made-ready.json";
        private const string StateAClientReadsAfterItsGrant = "state-a-client-reads-after-its-grant.json";

        private const string UsageDataPage = "docs/settings/usagedata.md";
        private const string EveryEventCarriesHeading = "Every event carries these";
        private const string SourceRowName = "Source";

        private static readonly string[] OneForecastRun = [ForecastRun];

        private static readonly string[] EveryLabelledSource = [Browser, Cli, Mcp];

        private static readonly string[] EverythingTheStateCarriesOnceItLabelsSources =
            ["sending", "decision", "mayAsk", "reAskAfterDays", "administratorDisabled", "acceptedSources"];

        private static readonly ForwardedMessage[] TheVoteLhReported =
        [
            new("TeamSizingVoteCast", Cli, "OnRefinementDay"),
            new("TeamSizingReadinessReached", Cli, "OnRefinementDay"),
        ];

        private sealed record ForwardedMessage(string Event, string? Source, string? SizingMoment);

        private static string ABatchNamingNoSource(params string[] eventNames)
        {
            return ABatchFrom(SourceLeftOut, eventNames);
        }

        private static string ABatchDeclaring(string source, params string[] eventNames)
        {
            return ABatchFrom($"\"{source}\"", eventNames);
        }

        /// <summary>
        /// A batch of events that happen on no particular page, with the source written exactly as
        /// <paramref name="sourceOnTheWire"/> says: a JSON value, the word null, or left out altogether.
        /// </summary>
        private static string ABatchFrom(string sourceOnTheWire, params string[] eventNames)
        {
            var events = string.Join(',', eventNames.Select((name, sequence) =>
                $"{{\"name\":\"{name}\",\"offsetMs\":0,\"sequence\":{sequence}}}"));

            return sourceOnTheWire == SourceLeftOut
                ? $"{{\"events\":[{events}]}}"
                : $"{{\"source\":{sourceOnTheWire},\"events\":[{events}]}}";
        }

        private async Task<string?> ATokenThatIs(string whichToken)
        {
            switch (whichToken)
            {
                case WithNoToken:
                    return null;
                case WithATokenNeverMintedHere:
                    return "neverMintedHere";
                case WithAGrantedToken:
                    return await ABrowserThatAgreedAsync();
                default:
                    var token = await ABrowserThatAgreedAsync();
                    await WithdrawAsync(token);
                    return token;
            }
        }

        private async Task WithdrawAsync(string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, ConsentRoute);
            request.Headers.Add(ConsentTokenHeader, token);
            using var response = await Client.SendAsync(request);
        }

        /// <summary>
        /// Every message the collector was handed, read out of the bytes that left: its event, the source
        /// it carried, and when it happened in refinement. A message with no source reads as null.
        /// </summary>
        private async Task<List<ForwardedMessage>> WhatTheCollectorWasHanded()
        {
            await EverythingTheCollectorReceived();

            return [.. Outbound.ThatReached(CollectorHost).SelectMany(request => MessagesIn(request.Body))];
        }

        private static List<ForwardedMessage> MessagesIn(string body)
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind is not JsonValueKind.Array)
            {
                return [];
            }

            return [.. document.RootElement.EnumerateArray().Select(message =>
                new ForwardedMessage(
                    message.GetProperty("event").GetString() ?? string.Empty,
                    ThePropertyIn(message, "source"),
                    ThePropertyIn(message, "sizing_moment")))];
        }

        private static string? ThePropertyIn(JsonElement message, string name)
        {
            return message.TryGetProperty("properties", out var properties)
                && properties.ValueKind is JsonValueKind.Object
                && properties.TryGetProperty(name, out var value)
                && value.ValueKind is JsonValueKind.String
                    ? value.GetString()
                    : null;
        }

        private static void ThenEveryMessageCarriesTheSource(List<ForwardedMessage> forwarded, string source)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(forwarded, Is.Not.Empty,
                    "nothing reached the collector, so 'every message carries its source' would hold for a "
                    + "pipe that forwards nothing at all");
                Assert.That(forwarded.Select(message => message.Source), Is.All.EqualTo(source),
                    $"a forwarded message did not carry source {source}; the maintainer would count it "
                    + "under the wrong surface, or under none");
            }
        }

        private async Task<string> TheStateBodyServedTo(string? token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, StateRoute);

            if (token is not null)
            {
                request.Headers.Add(ConsentTokenHeader, token);
            }

            using var response = await Client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        private async Task<List<string>> TheSourcesTheStateSaysThisLighthouseLabels(string? token)
        {
            var state = await TheStateThisBrowserIsToldAsync(token);

            if (!state.TryGetProperty("acceptedSources", out var accepted)
                || accepted.ValueKind is not JsonValueKind.Array)
            {
                return [];
            }

            return [.. accepted.EnumerateArray().Select(source => source.ToString())];
        }

        private async Task<List<string>> TheFieldsTheStateCarries(string? token)
        {
            var state = await TheStateThisBrowserIsToldAsync(token);
            return [.. state.EnumerateObject().Select(property => property.Name)];
        }

        private static bool TheSameJson(string actual, string expected)
        {
            return JsonNode.DeepEquals(JsonNode.Parse(actual), JsonNode.Parse(expected));
        }

        /// <summary>
        /// Read from the source tree rather than the build output, so the fixture is the file the clients
        /// copy, byte for byte, and adding one needs no change to the project file.
        /// </summary>
        private static string TheFixture(string name)
        {
            var path = Path.Combine(
                TheBackendSolutionDirectory(), "Lighthouse.Backend.Tests", "Integration", "UsageData", "Fixtures", name);

            Assert.That(File.Exists(path), Is.True,
                $"the contract fixture {name} is missing; it is one half of the contract with the clients");

            return File.ReadAllText(path);
        }

        /// <summary>
        /// The row of the page's "every event carries these" table whose first cell is
        /// <paramref name="rowName"/>, or null when there is none.
        /// </summary>
        private static string? TheRowOfWhatEveryEventCarriesNamed(string rowName)
        {
            var lines = TheUsageDataPage().Split('\n');
            var heading = Array.FindIndex(lines, line => line.Contains(EveryEventCarriesHeading, StringComparison.Ordinal));

            Assert.That(heading, Is.GreaterThanOrEqualTo(0),
                $"'{EveryEventCarriesHeading}' is no longer on the page, so there is no table to read.");

            var tableStarted = false;
            for (var line = heading + 1; line < lines.Length; line++)
            {
                var text = lines[line].Trim();
                if (!text.StartsWith('|'))
                {
                    if (tableStarted)
                    {
                        break;
                    }

                    continue;
                }

                tableStarted = true;
                var firstCell = text.Trim('|').Split('|')[0].Trim();
                if (firstCell.Equals(rowName, StringComparison.Ordinal))
                {
                    return text;
                }
            }

            return null;
        }

        private static string TheUsageDataPage()
        {
            var page = Path.Combine(
                Directory.GetParent(TheBackendSolutionDirectory())!.FullName,
                UsageDataPage.Replace('/', Path.DirectorySeparatorChar));

            return File.ReadAllText(page);
        }

        private static string TheBackendSolutionDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the read.");

            return directory!.FullName;
        }
    }
}
