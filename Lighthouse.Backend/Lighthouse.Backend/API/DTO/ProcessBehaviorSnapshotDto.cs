using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// Wire shape of one recorded process-behaviour day (ADR-108): the dated limit triple the PBC Over
    /// Time widget plots. Deliberately its own DTO rather than a fold into
    /// <see cref="PercentilesOverTimeSnapshotDto"/> or a polymorphic envelope — both were rejected in
    /// ADR-108, because a limit triple and a percentile quartet are different contracts that evolve apart.
    /// The metric family is carried by the request's <c>type</c> parameter, not repeated per row.
    /// </summary>
    public class ProcessBehaviorSnapshotDto
    {
        public ProcessBehaviorSnapshotDto()
        {
        }

        public ProcessBehaviorSnapshotDto(ProcessBehaviorSnapshot snapshot)
        {
            RecordedAt = snapshot.RecordedAt.ToString("yyyy-MM-dd");
            Unpl = snapshot.Unpl;
            Average = snapshot.Average;
            Lnpl = snapshot.Lnpl;
        }

        public string RecordedAt { get; set; } = string.Empty;

        public int Unpl { get; set; }

        public int Average { get; set; }

        public int Lnpl { get; set; }
    }
}
