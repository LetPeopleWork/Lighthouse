
namespace CMFTAspNet.Models.Forecast
{
    public class NoForecast : WhenForecast
    {
        public NoForecast() : base()
        {            
        }

        public NoForecast(int numberOfItems) : base(new Dictionary<int, int>(), numberOfItems)
        {
        }

        public override double GetLikelihood(int daysToTargetDate)
        {
            return 0;
        }
    }
}
