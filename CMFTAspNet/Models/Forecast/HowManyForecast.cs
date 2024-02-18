namespace CMFTAspNet.Models.Forecast
{
    public class HowManyForecast : ForecastBase
    {
        public HowManyForecast() : base()
        {            
        }


        public HowManyForecast(Dictionary<int, int> simulationResult) : base(simulationResult, Comparer<int>.Create((x, y) => y.CompareTo(x)))
        {
        }
    }
}