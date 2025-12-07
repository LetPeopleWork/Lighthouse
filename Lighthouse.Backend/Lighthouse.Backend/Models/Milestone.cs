﻿namespace Lighthouse.Backend.Models
{
    public class Milestone
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime Date { get; set; }

        public Portfolio? Portfolio { get; set; }

        public int PortfolioId { get; set; }
    }
}
