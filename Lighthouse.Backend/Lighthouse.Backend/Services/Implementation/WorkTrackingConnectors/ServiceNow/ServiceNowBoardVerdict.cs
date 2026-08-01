using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // ADR-126 decision 3. A board read meets the same table the connection ladder was written for, so
    // every rung of ServiceNowValidationVerdict.FromResponse is called through rather than copied.
    // Exactly one rung is intercepted, and only for the list.
    public static class ServiceNowBoardVerdict
    {
        // Both causes named, neither asserted — the house style no_records_visible established.
        // X-Total-Count is ACL-blind on vtb_board (header 2, body 0, measured 2026-08-01), so
        // nothing on the platform can separate them.
        private const string NoBoardsAvailableMessage =
            "No ServiceNow boards are available to this connection. Either the account this connection signs in " +
            "with is not a member of any Visual Task Board, or none of its boards has both a table and a filter " +
            "set — Lighthouse can only use a board that has both. Share a board with that account in ServiceNow, " +
            "or set a filter on one, and try again.";

        /// <summary>
        /// The board list. A 200 with no rows is the one rung this ladder does not inherit: boards
        /// are shared rather than roled, so an account nobody has shared a board with reads zero
        /// rows, and <c>no_records_visible</c> would report an action the customer can take as a
        /// fault they cannot. It is an empty list carrying the reason, not a failure.
        /// </summary>
        public static ConnectionValidationResult FromBoardList(
            HttpStatusCode statusCode, bool carriesRecords, int boardCount, string table)
        {
            if (statusCode == HttpStatusCode.OK && carriesRecords && boardCount < 1)
            {
                return ConnectionValidationResult.SuccessWith("no_boards_available", NoBoardsAvailableMessage);
            }

            return ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, boardCount, table);
        }

        /// <summary>
        /// One board, read again under the same scoping the list applied (ADR-125 decision 3). Here
        /// the empty rung is inherited: a board that stopped carrying a table or a filter between
        /// the list and the pick has no query to hand over, and handing over blanks is the silent
        /// failure the scoping exists to prevent.
        /// </summary>
        public static ConnectionValidationResult FromBoardRead(
            HttpStatusCode statusCode, bool carriesRecords, int boardCount, string table)
        {
            return ServiceNowValidationVerdict.FromResponse(statusCode, carriesRecords, boardCount, table);
        }
    }
}
