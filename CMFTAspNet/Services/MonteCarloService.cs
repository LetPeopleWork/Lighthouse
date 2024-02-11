
using CMFTAspNet.Models;

namespace CMFTAspNet.Services
{
    public class MonteCarloService
    {
        private int trials;

        public MonteCarloService()
        {
            trials = 10000;
        }

        public HowManyForecast HowMany(Throughput throughput, int days)
        {
            var simulationResult = new Dictionary<int, int>();

            for (var trial = 0; trial < trials; trial++)
            {
                var simulatedThroughput = 0;
                for (var day = 0; day < days; day++)
                {
                    var randomDay = new Random().Next(throughput.History - 1);
                    simulatedThroughput += throughput.GetThroughputOnDay(randomDay);                    
                }

                if (!simulationResult.ContainsKey(simulatedThroughput))
                {
                    simulationResult[simulatedThroughput] = 0;
                }

                simulationResult[simulatedThroughput]++;
            }

            return new HowManyForecast(simulationResult);
        }
    }
}
