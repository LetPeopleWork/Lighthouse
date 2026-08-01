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

        /// <summary>
        /// A lane's state label, which is the form Lighthouse's state mapping is written in. The
        /// lane's <c>value</c> is the compound <c>&lt;table&gt;:&lt;n&gt;</c> form and is a list of
        /// several classes on change_request, so it is not read here.
        /// </summary>
        private const string LaneNameField = "name";

        /// <summary>
        /// Lane names that hold work which never completed. A heuristic and nothing more: ruling R-4
        /// found <c>sys_choice</c> admin-only, so below <c>itil</c> no metadata marks a state as a
        /// cancellation and the label is all there is to match on.
        /// </summary>
        private static readonly string[] LanesOutsideTheFlow = ["Canceled", "Cancelled"];

        /// <summary>
        /// Below this there is no split to make — one lane cannot start and end the flow at once, and
        /// two leave nothing under way in between.
        /// </summary>
        private const int FewestLanesAFlowCanHave = 3;

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
        /// The pre-fill: the board's filter becomes the team's query, the board's table becomes the
        /// kind of work it handles and the board's lanes become the states its work moves through
        /// (ADR-125 decision 1). All of it stays editable — the board is a starting point, not a
        /// binding. The lanes arrive in the order the board arranged them.
        /// </summary>
        /// <remarks>
        /// Every value handed over is in the words the coach reads: the lane names already are, and
        /// the table is labelled through <see cref="ServiceNowClassLabels.LabelFor"/> (#5610 OC-4).
        /// #5612 deleted the save-time normalisation, so a class name pre-filled here would stay a
        /// class name while that team's work items report the words it was configured with — a team
        /// that syncs and forecasts nothing. <see cref="KindOfWorkOn"/> stays raw for the ladder.
        /// </remarks>
        public static BoardInformation ToBoardInformation(JsonElement row, List<JsonElement> lanes)
        {
            var flow = TheFlowAcross(lanes);

            return new BoardInformation
            {
                DataRetrievalValue = ServiceNowWorkItemMapper.ReadForm(
                    row, BoardFilterField, ServiceNowWorkItemMapper.UniversalForm),
                WorkItemTypes = [ServiceNowClassLabels.LabelFor(KindOfWorkOn(row))],
                ToDoStates = flow.WorkStartsIn,
                DoingStates = flow.WorkIsUnderWayIn,
                DoneStates = flow.WorkEndsIn,
            };
        }

        /// <summary>
        /// A board's columns are its flow: work starts in the first, ends in the last and is under way
        /// everywhere between. Too few lanes to say that means saying nothing — an invented split is
        /// worse than the empty lists that send the administrator to map the states by hand.
        /// </summary>
        /// <remarks>
        /// On the stock incident board this puts Resolved under way, which is the convention ADR-117
        /// already settled on for the same table. Agreement, not a rule written for that one board.
        /// </remarks>
        private static Flow TheFlowAcross(List<JsonElement> lanes)
        {
            var inFlow = lanes
                .Select(lane => ServiceNowWorkItemMapper.ReadForm(
                    lane, LaneNameField, ServiceNowWorkItemMapper.ReadableForm))
                .Where(IsPartOfTheFlow)
                .ToList();

            if (inFlow.Count < FewestLanesAFlowCanHave)
            {
                return new Flow([], [], []);
            }

            return new Flow([inFlow[0]], inFlow.GetRange(1, inFlow.Count - 2), [inFlow[^1]]);
        }

        // Matched wherever the lane sits rather than only last: a board is free to park cancelled work
        // in the middle, and there it would otherwise be donated to Doing.
        private static bool IsPartOfTheFlow(string laneName)
        {
            return !string.IsNullOrWhiteSpace(laneName)
                && !LanesOutsideTheFlow.Contains(laneName.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        private sealed record Flow(List<string> WorkStartsIn, List<string> WorkIsUnderWayIn, List<string> WorkEndsIn);

        /// <summary>The board's own table, which is the candidate class ADR-124's ladder judges.</summary>
        public static string KindOfWorkOn(JsonElement row)
        {
            return ServiceNowWorkItemMapper.ReadForm(
                row, BoardTableField, ServiceNowWorkItemMapper.UniversalForm);
        }
    }
}
