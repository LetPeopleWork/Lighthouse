using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// Reading, out of a work item's relations, the other items it is waiting on.
    ///
    /// Azure DevOps records a dependency twice - once on each end. The item that has to wait carries a
    /// Predecessor link, and the item being waited on carries the mirror Successor link. Only one of those
    /// two ends is read: the other is the same edge seen from the far side, so taking both would count
    /// every dependency in the instance twice.
    ///
    /// Which end is which is decided by the link type, not by the word Azure DevOps prints beside it. That
    /// word is localised and can be renamed by a project administrator; the link type cannot.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsDependencyRelationTest
    {
        private const string Predecessor = "System.LinkTypes.Dependency-Reverse";

        private const string Successor = "System.LinkTypes.Dependency-Forward";

        private const string Parent = "System.LinkTypes.Hierarchy-Reverse";

        private static readonly string[] TheOneItemItWaitsOn = ["1801"];

        private static readonly string[] TheTwoItemsItWaitsOn = ["1801", "1799"];

        [Test]
        public void ExtractDependencyReferences_YieldsTheItemAPredecessorPointsAt()
        {
            var workItem = AWorkItemRelatedTo(APredecessorPointingAt(1801));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheOneItemItWaitsOn));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingForAnItemThatOnlyHasSuccessors()
        {
            var workItem = AWorkItemRelatedTo(ASuccessorPointingAt(3540));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty,
                "A Successor is the far end of somebody else's wait. Reading it here as if it were this item's "
                + "own would record every dependency a second time, pointing the wrong way.");
        }

        [Test]
        public void ExtractDependencyReferences_YieldsOnlyThePredecessorsWhenBothDirectionsAreLinked()
        {
            var workItem = AWorkItemRelatedTo(
                APredecessorPointingAt(1801),
                ASuccessorPointingAt(3540),
                APredecessorPointingAt(1799),
                ASuccessorPointingAt(3533),
                ASuccessorPointingAt(3512),
                AParentPointingAt(1700));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheTwoItemsItWaitsOn));
        }

        [Test]
        public void ExtractDependencyReferences_ReadsTheLinkTypeRatherThanTheDisplayName()
        {
            var workItem = AWorkItemRelatedTo(ARelation(Predecessor, TheUrlOf(1801), "Vorgaenger"));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheOneItemItWaitsOn),
                "The name beside a link is localised and renameable by a project administrator, so a German "
                + "project would lose every dependency it has if the name were what decided this.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("nowhere")]
        [TestCase("https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/")]
        [TestCase("https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/not-an-id")]
        public void ExtractDependencyReferences_SkipsARelationWhoseUrlNamesNoWorkItem(string? url)
        {
            var workItem = AWorkItemRelatedTo(ARelation(Predecessor, url, "Predecessor"));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty,
                "A link nobody can read has to leave no trace at all. Recording the readable half of it would "
                + "claim a wait on an item that does not exist, and throwing would abandon the rest of the "
                + "refresh over one bad link.");
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingWhenTheItemCarriesNoRelationsAtAll()
        {
            var workItem = new WorkItem();

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        private static WorkItem AWorkItemRelatedTo(params WorkItemRelation[] relations)
        {
            return new WorkItem { Relations = relations.ToList() };
        }

        private static WorkItemRelation APredecessorPointingAt(int id)
        {
            return ARelation(Predecessor, TheUrlOf(id), "Predecessor");
        }

        private static WorkItemRelation ASuccessorPointingAt(int id)
        {
            return ARelation(Successor, TheUrlOf(id), "Successor");
        }

        private static WorkItemRelation AParentPointingAt(int id)
        {
            return ARelation(Parent, TheUrlOf(id), "Parent");
        }

        private static WorkItemRelation ARelation(string rel, string? url, string displayName)
        {
            return new WorkItemRelation
            {
                Rel = rel,
                Url = url,
                Attributes = new Dictionary<string, object> { { "name", displayName } },
            };
        }

        private static string TheUrlOf(int id)
        {
            return $"https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/{id}";
        }
    }
}
