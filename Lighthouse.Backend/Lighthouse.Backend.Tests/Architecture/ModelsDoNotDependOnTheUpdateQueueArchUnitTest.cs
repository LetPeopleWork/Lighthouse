using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A forecast can be stopped part-way, and the stop travels as a token handed down from the update
    /// that runs it. The token is passed into the forecast; the domain model never goes looking for it. A
    /// Model that reached into the update queue would know which update it is running under, and a Feature
    /// or a forecast would then behave differently depending on who asked for it.
    ///
    /// UpdateType is the one exception. RefreshSubject in the Models uses it to say what a refresh-history
    /// entry is about (Team, Features, Forecasts). It is a plain name that says nothing about any
    /// update in progress; it is only declared in the update queue's namespace, not part of its state.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-02")]
    public class ModelsDoNotDependOnTheUpdateQueueArchUnitTest
    {
        private const string TheModels = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string TheUpdateQueue = @"^Lighthouse\.Backend\.Services\.Implementation\.BackgroundServices\.Update(\.|$)";

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        [Test]
        public void TheDomainModel_KnowsNothingOfTheUpdateItRunsUnder()
        {
            Types().That().ResideInNamespaceMatching(TheModels)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(TheUpdateQueue).And().AreNot(typeof(UpdateType)))
                .Because(
                    "stopping a forecast is the update's business: it hands the forecast a token to stop on. A " +
                    "Model that reads the update queue's own state decides things by which update it runs " +
                    "under, which nothing testing the Model on its own would ever see.")
                .Check(Architecture);
        }
    }
}
