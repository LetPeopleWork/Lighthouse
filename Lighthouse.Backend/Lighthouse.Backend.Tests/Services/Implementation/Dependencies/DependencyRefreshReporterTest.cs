using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    /// <summary>
    /// What an operator reading a log file hears about a refresh's dependencies. Everything here is also on
    /// screen for the user; it is in the log so a support conversation can be had from a log file rather
    /// than from a screenshot.
    /// </summary>
    [TestFixture]
    [Category("epic-5792-dependency-aware-forecasting")]
    public class DependencyRefreshReporterTest
    {
        private const string TheTeamsName = "Delivery";

        [Test]
        public void AnUnlicensedInstance_IsToldOnceThatItsDatesIgnoreWhatFeaturesAreWaitingOn()
        {
            var logger = new Mock<ILogger<DependencyRefreshReporter>>();

            ReportOn(APortfolioWhereOneFeatureWaitsOnAnother(), hasPremiumLicence: false, logger);

            Assert.That(TheWarnings(logger), Has.Count.EqualTo(1),
                "An operator reading logs rather than screens has no other way to learn that the dates in " +
                "front of them were worked out as though nothing waited on anything.");
        }

        [Test]
        public void TheLineAnUnlicensedInstanceGets_NamesTheTeamsWhoseDatesReadWrong()
        {
            var logger = new Mock<ILogger<DependencyRefreshReporter>>();

            ReportOn(APortfolioWhereOneFeatureWaitsOnAnother(), hasPremiumLicence: false, logger);

            Assert.That(TheWarnings(logger).Single(), Does.Contain(TheTeamsName),
                "Knowing that some dates are wrong is not the same as knowing whose.");
        }

        /// <summary>
        /// A count dropped straight into a plural noun reads "1 Features". These lines get read out loud in
        /// support conversations, which is exactly how somebody notices.
        /// </summary>
        [Test]
        public void TheLineAboutOneFeature_SaysFeatureRatherThanFeatures()
        {
            var logger = new Mock<ILogger<DependencyRefreshReporter>>();

            ReportOn(APortfolioWhereOneFeatureWaitsOnAnother(), hasPremiumLicence: false, logger);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheWarnings(logger).Single(), Does.Contain("1 Feature"));
                Assert.That(TheWarnings(logger).Single(), Does.Not.Contain("1 Features"));
            }
        }

        [Test]
        public void ALicensedInstance_IsToldNothing()
        {
            var logger = new Mock<ILogger<DependencyRefreshReporter>>();

            ReportOn(APortfolioWhereOneFeatureWaitsOnAnother(), hasPremiumLicence: true, logger);

            Assert.That(TheWarnings(logger), Is.Empty,
                "Nothing is being held back, so there is nothing to say and a line saying it would teach an " +
                "operator to skip lines.");
        }

        /// <summary>
        /// Somebody switched these off. Telling them their dates are wrong because of a licence would send
        /// them to buy back a thing they turned off on purpose.
        /// </summary>
        [Test]
        public void APortfolioThatSetItsDependenciesAside_IsToldNothingEitherWayAboutTheLicence()
        {
            var logger = new Mock<ILogger<DependencyRefreshReporter>>();

            var portfolio = APortfolioWhereOneFeatureWaitsOnAnother();
            portfolio.IgnoreDependencies = true;

            ReportOn(portfolio, hasPremiumLicence: false, logger);

            Assert.That(TheWarnings(logger), Is.Empty);
        }

        private static void ReportOn(
            Portfolio portfolio, bool hasPremiumLicence, Mock<ILogger<DependencyRefreshReporter>> logger)
        {
            var licenseService = new Mock<ILicenseService>();
            licenseService.Setup(licence => licence.CanUsePremiumFeatures()).Returns(hasPremiumLicence);

            var reporter = new DependencyRefreshReporter(
                new DependencyDecision(new DependencyHonourPolicy(), licenseService.Object),
                logger.Object);

            reporter.ReportOn(portfolio);
        }

        private static Portfolio APortfolioWhereOneFeatureWaitsOnAnother()
        {
            var team = new Team { Id = 1, Name = TheTeamsName };
            var portfolio = new Portfolio { Id = 1, Name = "Platform" };

            var blocker = AFeature(2, "F-2", team);
            var dependent = AFeature(1, "F-1", team);
            dependent.ReplaceDependsOnReferences(
                [new FeatureDependencyReference(dependent.Id, blocker.ReferenceId, Backend.Models.Dependencies.DependencySource.TrackerLink)]);

            portfolio.UpdateFeatures([dependent, blocker]);

            // The other half of the relation, which the store fills in and an in-memory Portfolio does not.
            // Without it the two Features share no Portfolio and every dependency between them reads as
            // reaching outside one - which is a true answer to a fixture that never said where they live.
            dependent.Portfolios.Add(portfolio);
            blocker.Portfolios.Add(portfolio);

            return portfolio;
        }

        private static Feature AFeature(int id, string referenceId, Team team)
        {
            var feature = new Feature(team, 5) { Id = id, Name = $"Feature {id}", ReferenceId = referenceId };

            // A Feature with nothing simulated for it cannot be forecast, and a dependency on one reads as
            // that rather than as anything about a licence. The trials are what make this a forecast.
            var simulated = new SimulationResult(team, feature, 0);
            simulated.SimulationResults.Add(5, 10);
            feature.Forecasts.Add(new Backend.Models.Forecast.WhenForecast(simulated));

            return feature;
        }

        private static List<string> TheWarnings(Mock<ILogger<DependencyRefreshReporter>> logger)
            => logger.Invocations
                .Where(invocation => invocation.Arguments.Contains(LogLevel.Warning))
                .Select(invocation => invocation.Arguments[2]?.ToString() ?? string.Empty)
                .ToList();
    }
}
