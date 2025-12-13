using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Models
{
    public class Delivery : IEntity
    {
        public int Id { get; set; }
        
        public string Name { get; set; }
        
        public DateTime Date { get; set; }
        
        public int PortfolioId { get; set; }
        
        public Portfolio? Portfolio { get; set; }
        
        public List<Feature> Features { get; } = new List<Feature>();

        public Delivery(string name, DateTime date, int portfolioId)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty");
            }

            if (date <= DateTime.UtcNow)
            {
                throw new ArgumentException("Delivery date must be in the future");
            }

            Name = name;
            Date = date;
            PortfolioId = portfolioId;
        }

        // Parameterless constructor for EF Core
        public Delivery()
        {
            Name = string.Empty;
        }
    }
}