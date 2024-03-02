namespace CMFTAspNet.Models
{
    public class Milestone
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public Project? Project { get; set; }

        public int ProjectId { get; set; }
    }
}
