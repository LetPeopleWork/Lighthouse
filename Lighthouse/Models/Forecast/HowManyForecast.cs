namespace Lighthouse.Models.Forecast
{
    public class HowManyForecast : ForecastBase
    {
        private readonly int days;

        public HowManyForecast() : base(Comparer<int>.Create((x, y) => y.CompareTo(x)))
        {
        }


        public HowManyForecast(Dictionary<int, int> simulationResult, int days) : base(simulationResult, Comparer<int>.Create((x, y) => y.CompareTo(x)))
        {
            this.days = days;
        }

        public DateTime TargetDate
        {
            get
            {
                return CreationTime.AddDays(days);
            }
        }
    }
}