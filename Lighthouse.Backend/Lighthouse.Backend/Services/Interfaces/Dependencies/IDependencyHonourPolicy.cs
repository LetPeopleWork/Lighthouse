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
    /// <param name="PortfoliosSettingTheirDependenciesAside">
    /// Which Portfolios have asked for their dependencies not to be acted on. It is a fact the decision
    /// reads, in the same place as the licence, rather than a branch around ingestion: the edges are still
    /// read, stored and shown, and anything working from the answer never learns the setting exists.
    /// </param>
    public sealed record DependencyHonourInput(
        IReadOnlyCollection<FeatureDependencyFacts> FeaturesInScope,
        bool HasPremiumLicence,
        IReadOnlyCollection<int> PortfoliosSettingTheirDependenciesAside);

    /// <summary>
    /// One Feature as the decision sees it: where the user has put it, which Portfolios it belongs to,
    /// whether there is any forecast for it to be waited on for, and what it waits on. All of it read off
    /// what the caller already loaded, so asking costs no queries. The place is absent on read paths that
    /// never number the Features, and not knowing where something sits is not the same as knowing it sits
    /// below - so nothing is said about the order rather than everything being reported as out of order.
    ///
    /// Which Teams work a Feature used to be here too, because a wait between two Teams was one Lighthouse
    /// could not act on. Every Team is now forecast on one clock, so that is no longer a question the
    /// decision asks, and a fact nothing reads is a fact that goes quietly out of date.
    /// </summary>
    public sealed record FeatureDependencyFacts(
        string ReferenceId,
        IReadOnlyCollection<int> PortfolioIds,
        int? Position,
        bool CanBeForecast,
        IReadOnlyCollection<string> DependsOnReferenceIds);
}
