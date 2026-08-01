using System.Text.Json;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// The functional core of the board picker (ADR-114's shape): one <c>vtb_board</c> row in, one
    /// <see cref="Board"/> or one <see cref="BoardInformation"/> out. No IO, so every rule below is
    /// reachable as a unit test.
    /// </summary>
    /// <remarks>
    /// ADR-125 decision 2. A board holds its filter twice and only one of the two forms is usable:
    /// <c>filter</c> is the verbatim encoded query in column form and selects what the board shows,
    /// while the label form ServiceNow's own screen displays selects the WHOLE table — 105 of 105
    /// incidents, measured 2026-08-01. The label form has no name anywhere in this class, so a team
    /// pre-filled with it is not a bug that can be written here.
    /// </remarks>
    public static class ServiceNowBoardMapper
    {
        private const string BoardNameField = "name";

        private const string BoardTableField = "table";

        private const string BoardFilterField = "filter";

        public static Board ToBoard(JsonElement row)
        {
            return new Board
            {
                Id = ServiceNowWorkItemMapper.ReadForm(
                    row, ServiceNowWorkItemMapper.RecordIdField, ServiceNowWorkItemMapper.UniversalForm),
                Name = ServiceNowWorkItemMapper.ReadForm(row, BoardNameField, ServiceNowWorkItemMapper.ReadableForm),
            };
        }

        /// <summary>
        /// The pre-fill: the board's filter becomes the team's query and the board's table becomes
        /// the kind of work it handles (ADR-125 decision 1). Both stay editable — the board is a
        /// starting point, not a binding.
        /// </summary>
        public static BoardInformation ToBoardInformation(JsonElement row)
        {
            return new BoardInformation
            {
                DataRetrievalValue = ServiceNowWorkItemMapper.ReadForm(
                    row, BoardFilterField, ServiceNowWorkItemMapper.UniversalForm),
                WorkItemTypes = [KindOfWorkOn(row)],
            };
        }

        /// <summary>The board's own table, which is the candidate class ADR-124's ladder judges.</summary>
        public static string KindOfWorkOn(JsonElement row)
        {
            return ServiceNowWorkItemMapper.ReadForm(
                row, BoardTableField, ServiceNowWorkItemMapper.UniversalForm);
        }
    }
}
