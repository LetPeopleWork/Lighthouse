using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // ADR-126 decision 3. A board read meets the same table the connection ladder was written for, so
    // every rung of ServiceNowValidationVerdict.FromResponse is called through rather than copied.
    // Exactly one rung is intercepted, and only for the list.
    public static class ServiceNowBoardVerdict
    {
        private const string NoBoardsAvailableCode = "no_boards_available";

        // Both causes named, neither asserted — the house style no_records_visible established.
        // X-Total-Count is ACL-blind on vtb_board (header 2, body 0, measured 2026-08-01), so
        // nothing can separate them.
        private const string NoBoardsAvailableMessage =
            "No ServiceNow boards are available to this connection. Either the account this connection signs in " +
            "with is not a member of any Visual Task Board, or none of its boards has both a table and a filter " +
            "set — Lighthouse can only use a board that has both. Share a board with that account in ServiceNow, " +
            "or set a filter on one, and try again.";

        // Causes named, none asserted, as above: the scoped read cannot separate a cleared filter
        // from a deactivated board from one that stopped being shared. What they share is that the
        // board is no longer a query, which is the part worth saying.
        private const string BoardCannotBecomeAQueryMessage =
            "This ServiceNow board can no longer be turned into a team. Lighthouse can only use a board that is " +
            "active and carries both a table and a filter, and this one no longer does — its table or its filter " +
            "was cleared, it was deactivated, or it stopped being shared with the account this connection signs " +
            "in with. Set a table and a filter on it in ServiceNow, or pick another board.";

        /// <summary>
        /// The board list. A 200 with no rows is the one rung this ladder does not inherit: boards
        /// are shared rather than roled, so an account nobody has shared a board with reads zero
        /// rows, and <c>no_records_visible</c> would report an action the customer can take as a
        /// fault they cannot. It is an empty list carrying the reason, not a failure.
        /// </summary>
        /// <remarks>
        /// The reason rides <c>Code</c>/<c>Message</c> on a valid verdict, which is where every other
        /// rung already puts it. #5612 deleted <c>SuccessWith</c> and the Advisory pair it wrote to
        /// once they had no producer left; this rung is built here rather than reviving them.
        /// </remarks>
        public static ConnectionValidationResult FromBoardList(
            HttpStatusCode statusCode, bool carriesRecords, int boardCount, string table)
        {
            if (statusCode == HttpStatusCode.OK && carriesRecords && boardCount < 1)
            {
                return new ConnectionValidationResult
                {
                    IsValid = true,
                    Code = NoBoardsAvailableCode,
                    Message = NoBoardsAvailableMessage,
                };
            }

            return ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, boardCount, table);
        }

        /// <summary>
        /// One board, read again under the same scoping the list applied (ADR-125 decision 3). The
        /// second rung this ladder does not inherit, and for the opposite reason to the first: a
        /// board that stopped carrying a table or a filter between the list and the pick has no
        /// query to hand over, which is a refusal — but <c>no_records_visible</c> would advise
        /// granting a read role or report the table as empty, and neither is what happened. AC-B4
        /// wants the board refused by name, with the reason stated.
        /// </summary>
        public static ConnectionValidationResult FromBoardRead(
            HttpStatusCode statusCode, bool carriesRecords, int boardCount, string table)
        {
            if (statusCode == HttpStatusCode.OK && carriesRecords && boardCount < 1)
            {
                return ConnectionValidationResult.Failure(
                    "board_cannot_become_a_query",
                    BoardCannotBecomeAQueryMessage,
                    // Stryker disable once String: a support-log restatement of "the scoped single-board
                    // read answered 200 with no row". The message above is the half an administrator acts
                    // on, and PickingABoardThatNoLongerQualifies asserts it.
                    $"The scoped single-board read on '{table}' returned 200 with zero rows.");
            }

            return ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, boardCount, table);
        }
    }
}
