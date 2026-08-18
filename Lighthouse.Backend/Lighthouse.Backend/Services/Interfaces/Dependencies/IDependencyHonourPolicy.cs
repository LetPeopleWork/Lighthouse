using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Services.Interfaces.Dependencies
{
    /// <summary>
    /// The one place that decides whether Lighthouse can act on a dependency. Everything it is allowed to
    /// know arrives as the plain facts below, which is what keeps a screen showing a warning and anything
    /// later acting on that same dependency from ever telling the user two different things: they ask this,
    /// once, and read the same answer. Nothing here can be persuaded to load, log or write anything.
    /// </summary>
    public interface IDependencyHonourPolicy
    {
        HonouredDependencies Evaluate(DependencyHonourInput input);
    }

    /// <summary>
    /// Everything the decision is allowed to see. The licence flag is read by nothing today - letting a
    /// dependency change a forecast is paid behaviour that arrives separately - and it is here already so
    /// that arrival adds a rule inside one type rather than a parameter through every caller of it.
    /// </summary>
    public sealed record DependencyHonourInput(
        IReadOnlyCollection<FeatureDependencyFacts> FeaturesInScope,
        bool HasPremiumLicence);

    /// <summary>
    /// One Feature as the decision sees it: where the user has put it, which Portfolios it belongs to,
    /// whether there is any forecast for it to be waited on for, and what it waits on. All of it read off
    /// what the caller already loaded, so asking costs no queries.
    /// </summary>
    public sealed record FeatureDependencyFacts(
        string ReferenceId,
        IReadOnlyCollection<int> PortfolioIds,
        int Position,
        bool CanBeForecast,
        IReadOnlyCollection<string> DependsOnReferenceIds);
}
