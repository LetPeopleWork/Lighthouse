namespace Lighthouse.Backend.API.DTO
{
    public class WorkItemDto
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public string WorkItemReference { get; set; }

        public string Url { get; set; }

        public DateTime StartedDate { get; set; }

        public DateTime ClosedDate { get; set; }
    }
}
