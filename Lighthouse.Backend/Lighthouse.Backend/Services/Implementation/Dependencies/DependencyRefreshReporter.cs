using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Interfaces.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Says in the log what a refresh found among the dependencies it just read. It asks the one place
    /// that decides and reports the answer - the decision itself reaches no log, because something that
    /// records anything could answer the screen and a forecast differently.
    /// </summary>
    public class DependencyRefreshReporter(
        IDependencyDecision dependencyDecision,
        ILogger<DependencyRefreshReporter> logger) : IDependencyRefreshReporter
    {
        public void ReportOn(Portfolio portfolio)
        {
            var decided = dependencyDecision.About(portfolio.Features);

            ReportTheCircles(portfolio, decided);
            ReportWhatCannotBeForecast(portfolio, decided);
            ReportWhatAPremiumLicenceWouldHaveAccountedFor(portfolio, decided);
        }

        /// <summary>
        /// One line for a Portfolio whose dates are being read as though nothing waited on anything. An
        /// operator reading logs rather than screens has no other way to learn that, and it is the difference
        /// between a date they can plan against and one they cannot.
        ///
        /// It reads the set of waits held back rather than the licence, which is why it stays silent on a
        /// licensed instance and on a Portfolio that has set its dependencies aside: both of those present
        /// nothing held back, and a warning about an empty set is a line that teaches people to skip lines.
        /// </summary>
        private void ReportWhatAPremiumLicenceWouldHaveAccountedFor(Portfolio portfolio, HonouredDependencies decided)
        {
            var featuresHeldBack = decided.Verdicts
                .Where(verdict => verdict.Reason == NotHonouredReason.NotLicensed)
                .Select(verdict => verdict.DependentReferenceId)
                .ToHashSet(StringComparer.Ordinal);

            if (featuresHeldBack.Count == 0)
            {
                return;
            }

            var teamsReadingWrong = portfolio.Features
                .Where(feature => featuresHeldBack.Contains(feature.ReferenceId))
                .SelectMany(feature => feature.Teams)
                .Select(team => team.Name)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            logger.LogWarning(
                "Dates in Portfolio {PortfolioName} read as though nothing were waiting on anything: this instance is not licensed to account for what {Count} Features are waiting on. Teams affected: {Teams}",
                portfolio.Name,
                featuresHeldBack.Count,
                string.Join(", ", teamsReadingWrong));
        }

        /// <summary>
        /// One line for every Feature caught in a circle, rather than one per circle or one per link. A
        /// circle is rare and genuinely wrong, so the operator wants to be told; being told the same thing
        /// once per link round the circle is how a rare event starts reading as noise.
        /// </summary>
        private void ReportTheCircles(Portfolio portfolio, HonouredDependencies decided)
        {
            var caughtInACircle = TheFeaturesWhere(decided, NotHonouredReason.InALoop);

            if (caughtInACircle.Count == 0)
            {
                return;
            }

            logger.LogWarning(
                "Features in Portfolio {PortfolioName} are waiting on each other in a circle, so those dependencies are left out: {Features}",
                portfolio.Name,
                string.Join(", ", caughtInACircle));
        }

        /// <summary>
        /// A count rather than a list of names. This is neither rare nor wrong - a Feature whose Team has
        /// nothing measured yet is an ordinary state of affairs - and the names are on screen for anyone
        /// who needs them.
        /// </summary>
        private void ReportWhatCannotBeForecast(Portfolio portfolio, HonouredDependencies decided)
        {
            var withoutADate = decided.Verdicts
                .Where(verdict => verdict.Reason == NotHonouredReason.BlockerCannotBeForecast)
                .Select(verdict => verdict.BlockerReferenceId)
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (withoutADate.Count == 0)
            {
                return;
            }

            logger.LogInformation(
                "Portfolio {PortfolioName} depends on {Count} Features that cannot be forecast, so those dependencies are left out",
                portfolio.Name,
                withoutADate.Count);
        }

        /// <summary>
        /// Both ends of every link the reason was given for. A circle is something a set of Features are in
        /// together, so naming only the waiting end would name half of it.
        /// </summary>
        private static List<string> TheFeaturesWhere(HonouredDependencies decided, NotHonouredReason reason)
        {
            return decided.Verdicts
                .Where(verdict => verdict.Reason == reason)
                .SelectMany(verdict => new[] { verdict.DependentReferenceId, verdict.BlockerReferenceId })
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();
        }
    }
}
