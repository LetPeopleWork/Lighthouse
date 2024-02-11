
using CMFTAspNet.Models;

namespace CMFTAspNet.Services
{
    public class ThroughputService
    {
        public Throughput GetThroughput()
        {
            return new Throughput([1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]);
        }
    }
}
