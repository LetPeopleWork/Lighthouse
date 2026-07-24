using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class PercentilesOverTimeSnapshotDto
    {
        public PercentilesOverTimeSnapshotDto()
        {
        }

        public PercentilesOverTimeSnapshotDto(PercentilesOverTimeSnapshot snapshot)
        {
            RecordedAt = snapshot.RecordedAt.ToString("yyyy-MM-dd");
            MetricType = snapshot.MetricType;
            P50 = snapshot.P50;
            P70 = snapshot.P70;
            P85 = snapshot.P85;
            P95 = snapshot.P95;
        }

        public string RecordedAt { get; set; } = string.Empty;

        public MetricType MetricType { get; set; }

        public int P50 { get; set; }

        public int P70 { get; set; }

        public int P85 { get; set; }

        public int P95 { get; set; }
    }
}
