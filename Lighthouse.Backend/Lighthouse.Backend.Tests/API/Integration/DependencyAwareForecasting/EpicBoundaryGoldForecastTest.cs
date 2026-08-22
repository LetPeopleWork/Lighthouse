namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    // The numbers in the gold file were produced by the released build, before any change to when a
    // forecast runs. Moving when a forecast runs must not move what it computes, and the only way to
    // show that is to compare against numbers taken before the move. Regenerating this file to make a
    // failure go away destroys the entire point of having it.
    //
    // That is one half of the promise. The other half - that refreshing everything now forecasts a
    // Portfolio once instead of once per Team - is checked by Slice00OneForecastPerBatchScenarios,
    // which drives a running application with a database and a work tracking connector behind it. A
    // second copy of that harness here would cost far more than keeping the two halves side by side.
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-00")]
    public class EpicBoundaryGoldForecastTest
    {
        [Test]
        public async Task PortfolioForecast_GoldFixture_MatchesPercentilesCapturedFromReleasedBuild()
        {
            var gold = GoldForecastFixture.ReadGoldSet();

            var actual = await new GoldForecastFixture().ForecastTheGoldPortfolio();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gold.RandomNumberSequence, Is.EqualTo(GoldForecastFixture.DrawSequence), "The gold set was captured with a different draw sequence, so its numbers are not comparable.");
                Assert.That(actual, Is.EqualTo(gold.Features), $"Forecast percentiles differ from the set captured at {gold.CapturedFrom}.");
            }
        }
    }
}
