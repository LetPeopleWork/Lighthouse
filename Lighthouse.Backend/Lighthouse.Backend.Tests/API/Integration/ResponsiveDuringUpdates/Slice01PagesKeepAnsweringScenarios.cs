using System.Net;

namespace Lighthouse.Backend.Tests.API.Integration.ResponsiveDuringUpdates
{
    /// <summary>
    /// Ingrid runs Lighthouse on a four-core host. While the "Platform Modernisation" forecast runs, she
    /// opens the Portfolios list and it answers, because the forecast takes at most two of the four cores.
    ///
    /// Nothing here measures time. The forecast is held part-way through its runs, the read is made while
    /// it is held, and what is asserted is that the read came back and how many workers the forecast held
    /// at that moment. How fast pages answer on real data is a measurement on the restored dev instance.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-01")]
    [Category("us-01")]
    public class Slice01PagesKeepAnsweringScenarios : ForecastWhileItUpdatesAcceptanceTest
    {
        private static readonly TimeSpan TheLongestAPageMayTake = TimeSpan.FromSeconds(10);

        [Test]
        [Ignore(PendingEpic6311.Slice01)]
        [Category("ac-01.1")]
        [Category("ac-01.2")]
        [Category("driving_port")]
        [Category("kpi-OUT-6311-KPI-1")]
        [Category("contract-shape:bounded-change")]
        public async Task The_Portfolios_list_answers_while_a_Portfolio_forecast_is_held_part_way_through()
        {
            var portfolio = GivenAPortfolioWithOneFeatureOfFiveItems();

            WhenThePortfolioForecastIsUnderWay(portfolio);
            var answer = await TheAnswerToAReadOfThePortfolios();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answer, Is.EqualTo(HttpStatusCode.OK),
                    "The Portfolios list did not answer while the forecast was running.");
                Assert.That(Draws.RunsBegun, Is.LessThan(TheRunsAForecastMakes),
                    "The forecast had finished by the time the page answered, so this proved nothing about answering during one.");
                Assert.That(Draws.MostRunsHeldAtOnce, Is.LessThanOrEqualTo(HalfTheCores),
                    "The forecast took more than half of the four cores while the page was being read.");
            }
        }

        private async Task<HttpStatusCode?> TheAnswerToAReadOfThePortfolios()
        {
            using var client = Factory.CreateClient();
            var read = client.GetAsync(new Uri("/api/latest/portfolios", UriKind.Relative));

            if (await Task.WhenAny(read, Task.Delay(TheLongestAPageMayTake)) != read)
            {
                return null;
            }

            using var response = await read;
            return response.StatusCode;
        }
    }
}
