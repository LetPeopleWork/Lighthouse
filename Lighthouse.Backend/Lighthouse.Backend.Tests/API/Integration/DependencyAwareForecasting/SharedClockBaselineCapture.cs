namespace Lighthouse.Backend.Tests.API.Integration.DependencyAwareForecasting
{
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    [Category("slice-02")]
    public class SharedClockBaselineCapture
    {
        [Test]
        [Explicit("Rewrites the recorded baseline. Only ever run on the commit that replaced where the forecast's numbers come from.")]
        public async Task RecordTheBaselineTheRestOfTheSliceIsHeldTo()
        {
            var percentiles = await SharedClockBaselineFixture.ForecastTheBenchmarkPortfolio();

            var path = await SharedClockBaselineFixture.WriteBaseline("slice-02, the addressable draw source", percentiles);

            Assert.That(File.Exists(path), Is.True, $"Failed to write the baseline to {path}.");
        }
    }
}
