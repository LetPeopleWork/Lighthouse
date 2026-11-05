using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Parents;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Parents
{
    /// <summary>
    /// Whether a list asks for the tracker's own parent to be read at all. Every tracker asks this one
    /// question: Jira before it looks for a parent field on Data Center, and Azure DevOps before it reads the
    /// relations of a work item, so that an install listing only fields makes no request it never made.
    /// </summary>
    [TestFixture]
    public class ParentSourceSelectorOwnParentTest
    {
        private static readonly ParentSourceEntry OwnParent = ParentSourceEntry.TrackerParent;

        private static IEnumerable<TestCaseData> ListsAndWhetherTheyAskForTheOwnParent()
        {
            yield return new TestCaseData(new[] { ParentSourceEntry.Field(11) }, false).SetName("Only a field: the own parent is not read");
            yield return new TestCaseData(new[] { ParentSourceEntry.Field(11), ParentSourceEntry.Field(12) }, false).SetName("Only fields: the own parent is not read");
            yield return new TestCaseData(new[] { OwnParent }, true).SetName("Only the own parent: it is read");
            yield return new TestCaseData(new[] { ParentSourceEntry.Field(11), OwnParent }, true).SetName("A field, then the own parent: it is read after the field");
            yield return new TestCaseData(new[] { OwnParent, ParentSourceEntry.Field(11) }, true).SetName("The own parent first: it is read first");
        }

        [TestCaseSource(nameof(ListsAndWhetherTheyAskForTheOwnParent))]
        public void The_tracker_reads_its_own_parent_only_when_the_list_holds_it(ParentSourceEntry[] entries, bool asksForIt)
        {
            var list = ParentSources.Of(entries).List!;

            Assert.That(ParentSourceSelector.ReadsTheTrackersOwnParent(list), Is.EqualTo(asksForIt));
        }
    }
}
