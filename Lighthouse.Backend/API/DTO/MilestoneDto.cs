using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class MilestoneDto
    {
        public MilestoneDto(Milestone milestone)
        {
            Id = milestone.Id;
            Name = milestone.Name;
            Date = milestone.Date;
        }

        public int Id { get; }

        public string Name { get; }

        public DateTime Date { get; }
    }
}
