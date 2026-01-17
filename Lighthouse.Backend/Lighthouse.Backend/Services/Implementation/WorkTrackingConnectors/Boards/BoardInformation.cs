namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards
{
    public class BoardInformation
    {
        public string DataRetrievalValue { get; set; } = string.Empty;

        public IEnumerable<string> WorkItemTypes { get; set; } = [];
        
        public IEnumerable<string> ToDoStates { get; set; } = [];
        
        public IEnumerable<string> DoingStates { get; set; } = [];
        
        public IEnumerable<string> DoneStates { get; set; } = [];
    }
}