namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-00")]
    public class GoldForecastCapture
    {
        [Test]
        [Explicit("Rewrites the committed gold file. Only ever run against a build that predates the change under test.")]
        public async Task CaptureGoldPercentiles()
        {
            var percentiles = await new GoldForecastFixture().ForecastTheGoldPortfolio();

            var path = await GoldForecastFixture.WriteGoldSet("4c0dea826", percentiles);

            Assert.That(File.Exists(path), Is.True, $"Failed to write the gold forecast set to {path}.");
        }
    }
}
