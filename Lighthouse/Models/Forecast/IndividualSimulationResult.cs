namespace Lighthouse.Models.Forecast
{
    public class IndividualSimulationResult
    {
        public int Id { get; set; }

        public int Key { get; set; }

        public int Value { get; set; }

        public ForecastBase Forecast { get; set; }

        public int ForecastId { get; set; } 
    }
}
