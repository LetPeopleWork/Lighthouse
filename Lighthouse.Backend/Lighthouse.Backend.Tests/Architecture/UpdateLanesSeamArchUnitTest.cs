using ArchUnitNET.NUnit;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The lanes are a queue and must not learn what they are running.
    ///
    /// A queue that could read an entity or call a work tracking system would be able to decide which
    /// work goes where on something other than the kind of work it is - and the moment routing depends
    /// on what the work turns out to contain, the promise that one kind of refresh cannot hold up
    /// another stops being something anyone can check by reading the routing table.
    /// </summary>
    [TestFixture]
    public class UpdateLanesSeamArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string UpdateLanesFullName = "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.UpdateLanes";

        private const string RepositoriesAndTheDatabase = @"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.Repositories)($|\..*)";

        private const string WorkTrackingConnectors = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.WorkTrackingConnectors($|\..*)";

        [Test]
        public void UpdateLanes_DoesNotDependOnRepositoriesTheDatabaseOrWorkTrackingConnectors()
        {
            Classes().That().HaveFullName(UpdateLanesFullName)
                .Should().NotDependOnAny(
                    Types().That().ResideInNamespaceMatching(RepositoriesAndTheDatabase)
                        .Or().ResideInNamespaceMatching(WorkTrackingConnectors))
                .Because(
                    "The lanes exist to keep one kind of work from holding up another, and they can only keep "
                    + "that promise while routing is decided from the update type alone. Reaching a repository, "
                    + "the database or a work tracking system would make which lane work runs in depend on what "
                    + "the work turns out to contain.")
                .Check(Architecture);
        }
    }
}
