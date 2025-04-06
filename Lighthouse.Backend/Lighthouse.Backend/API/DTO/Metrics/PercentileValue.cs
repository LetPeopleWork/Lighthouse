namespace Lighthouse.Backend.API.DTO.Metrics
{
    public class PercentileValue
    {
        public PercentileValue(int percentile, int value)
        {
            Percentile = percentile;
            Value = value;
        }

        public int Percentile { get; set; }

        public int Value { get; set; }
    }
}
