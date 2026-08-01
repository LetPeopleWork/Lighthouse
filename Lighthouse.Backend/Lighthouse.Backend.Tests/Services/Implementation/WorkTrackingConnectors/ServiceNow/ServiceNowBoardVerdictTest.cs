using System.Net;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // Story #5610 slice 02, AC-B3 / AC-B4. ADR-126 decision 3.
    //
    // The board ladder is a pure core that intercepts exactly two rungs of the connection ladder and
    // calls the rest through. ServiceNowBoardPickerTest exercises it through the connector, where an
    // empty list and a list of boards both leave the picker open — the interception carries an
    // advisory, not a refusal, and GetBoards only asks whether the verdict is valid. So the rung
    // itself is only observable here, at the core.
    [TestFixture]
    public class ServiceNowBoardVerdictTest
    {
        private const string TheBoardTable = "vtb_board";

        private const string NoBoardsAvailable = "no_boards_available";

        // DD-7. Boards are shared, not roled: an account nobody has shared a board with reads zero
        // rows, which is an empty list carrying its reason rather than the connection ladder's
        // no_records_visible — advice for a fault the customer does not have.
        [Test]
        public void ABoardListWithNoRowsAtAll_IsAnEmptyListCarryingItsReason()
        {
            var verdict = ServiceNowBoardVerdict.FromBoardList(HttpStatusCode.OK, carriesRecords: true, boardCount: 0, TheBoardTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True, "An account with no board shared with it has a picker to open, not a connection to fix.");
                Assert.That(verdict.AdvisoryCode, Is.EqualTo(NoBoardsAvailable));
                Assert.That(verdict.Advisory, Does.Contain("Visual Task Board").And.Contain("table and a filter"),
                    "Nothing on the instance can separate the two causes, so the copy names both and asserts neither.");
            }
        }

        // The rung is for a list that came back with nothing on it. One board is a board.
        [Test]
        public void ABoardListWithASingleBoardOnIt_IsNotAnEmptyList()
        {
            var verdict = ServiceNowBoardVerdict.FromBoardList(HttpStatusCode.OK, carriesRecords: true, boardCount: 1, TheBoardTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True);
                Assert.That(verdict.AdvisoryCode, Is.Null,
                    "A connection with exactly one usable board would otherwise be told it has none.");
            }
        }

        // A 200 whose body is not a record set is ADR-114's sign-in page, not an empty list and not a
        // board that stopped qualifying. Both interceptions have to let it past, or an SSO-only
        // account is reported as a customer who has not shared a board yet.
        [Test]
        public void ASuccessThatCarriesNoRecordSet_ReachesNeitherInterception()
        {
            var list = ServiceNowBoardVerdict.FromBoardList(HttpStatusCode.OK, carriesRecords: false, boardCount: 0, TheBoardTable);
            var read = ServiceNowBoardVerdict.FromBoardRead(HttpStatusCode.OK, carriesRecords: false, boardCount: 0, TheBoardTable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Code, Is.EqualTo("unexpected_response"));
                Assert.That(list.AdvisoryCode, Is.Null);
                Assert.That(read.Code, Is.EqualTo("unexpected_response"));
            }
        }
    }
}
