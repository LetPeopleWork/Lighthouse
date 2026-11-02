using Lighthouse.Backend.Models;

using static Lighthouse.Backend.Tests.TestHelpers.AzureDevOpsOrganisation;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// A stored list that still holds a group's name (not yet rewritten, skipped by the start-up pass, or saved by an
    /// older instance) is read through the owner's classifier, so an item in one of the group's states is still
    /// dated. A read that takes the stored list as it stands fails these.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsRawListsTest
    {
        private const int TheOnlyItem = 1;

        [Test]
        public async Task GetWorkItemsForTeam_StoredDoingListHoldsTheGroupName_DatesTheStartByTheArrivalIntoOneOfItsStates()
        {
            var (subject, team, ado) = AnAzureDevOpsThatHolds(TheOnlyItem);
            team.DoingStates = ["Active", "Review"];
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            ado.StateOfEveryItem = "Code Review";
            ado.StateHistoryOfEveryItem =
            [
                ("New", new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Unspecified)),
                ("Code Review", new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Unspecified)),
            ];

            var workItem = (await subject.GetWorkItemsForTeam(team, CancellationToken.None)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(workItem.StartedDate?.Date, Is.EqualTo(new DateTime(2026, 8, 5)));
                Assert.That(workItem.ClosedDate, Is.Null);
            }
        }
    }
}
