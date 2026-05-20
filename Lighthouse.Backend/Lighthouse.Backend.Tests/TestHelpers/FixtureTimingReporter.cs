using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests
{
    [SetUpFixture]
    public class FixtureTimingReporter
    {
        private static int reportWritten;

        [OneTimeSetUp]
        public void EnsureFallbackEmit()
        {
            if (!FixtureSetupTimer.Enabled)
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += (_, _) => TryEmit();
        }

        [OneTimeTearDown]
        public void EmitReport()
        {
            TryEmit();
            DisposeSharedFactory();
        }

        private static void DisposeSharedFactory()
        {
            if (IntegrationTestBase.TryGetSharedFactoryIfCreated(out var factory))
            {
                factory!.Dispose();
            }
        }

        private static void TryEmit()
        {
            if (!FixtureSetupTimer.Enabled)
            {
                return;
            }

            if (Interlocked.Exchange(ref reportWritten, 1) == 1)
            {
                return;
            }

            try
            {
                var outputPath = Environment.GetEnvironmentVariable("LIGHTHOUSE_FIXTURE_TIMING_OUT")
                    ?? Path.Combine(Path.GetTempPath(), "fixture-setup-timings.csv");

                var csv = FixtureSetupTimer.WriteReport(outputPath);
                TestContext.Out.WriteLine($"[FixtureSetupTimer] wrote {outputPath}");
                TestContext.Out.WriteLine(csv);
            }
            catch (Exception ex)
            {
                TestContext.Error.WriteLine($"[FixtureSetupTimer] failed to write report: {ex}");
            }
        }
    }
}
