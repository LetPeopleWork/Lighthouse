namespace Lighthouse.Backend.API.DTO
{
    public class StatesCollectionDto
    {
        public List<string> ToDoStates { get; set; } = [];

        public List<string> DoingStates {  get; set; } = [];

        public List<string> DoneStates { get; set; } = [];
    }
}