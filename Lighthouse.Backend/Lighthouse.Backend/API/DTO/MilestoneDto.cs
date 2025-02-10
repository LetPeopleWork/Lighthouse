using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class MilestoneDto
    {
        public MilestoneDto()
        {            
        }
        public MilestoneDto(Milestone milestone)
        {
            Id = milestone.Id;
            Name = milestone.Name;
            Date = milestone.Date;
        }

        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }
    }
}
