using System.Text.Json.Nodes;

namespace Lighthouse.Backend.Tests.API.Integration.ConnectionFieldChange
{
    /// <summary>
    /// Step definitions for the connection list saying who uses each connection. The two counts travel with the connection as
    /// <c>usedBy: { teams, portfolios }</c>, which is what the page's question is built from.
    /// </summary>
    public partial class Slice02ConnectionsSayWhoUsesThemTest : ConnectionFieldChangeAcceptanceTest
    {
        private Task<JsonObject> WhenPriyaOpensTheConnectionPage(string connection) => TheConnectionAsThePageReadsIt(connection);

        private static void ThenTheConnectionIsUsedBy(JsonObject read, int teams, int portfolios)
        {
            var usedBy = read["usedBy"] as JsonObject;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(usedBy?["teams"]?.GetValue<int>(), Is.EqualTo(teams), $"How many Teams read through it: {read.ToJsonString()}");
                Assert.That(usedBy?["portfolios"]?.GetValue<int>(), Is.EqualTo(portfolios), $"How many Portfolios read through it: {read.ToJsonString()}");
            }
        }
    }
}
