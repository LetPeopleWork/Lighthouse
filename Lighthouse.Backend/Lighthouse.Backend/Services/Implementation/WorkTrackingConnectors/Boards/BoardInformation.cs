namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards
{
    public class BoardInformation
    {
        public string DataRetrievalValue { get; set; }
        
        public IEnumerable<string> WorkItemTypes { get; set; }
        
        public IEnumerable<string> ToDoStates { get; set; }
        
        public IEnumerable<string> DoingStates { get; set; }
        
        public IEnumerable<string> DoneStates { get; set; }
    }
}