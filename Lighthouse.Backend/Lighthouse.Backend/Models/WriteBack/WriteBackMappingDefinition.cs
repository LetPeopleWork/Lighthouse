namespace Lighthouse.Backend.Models.WriteBack
{
    public class WriteBackMappingDefinition
    {
        public int Id { get; set; }

        public WriteBackValueSource ValueSource { get; set; }

        public WriteBackAppliesTo AppliesTo { get; set; }

        public string TargetFieldReference { get; set; } = string.Empty;

        public WriteBackTargetValueType TargetValueType { get; set; } = WriteBackTargetValueType.Date;

        public string? DateFormat { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection? WorkTrackingSystemConnection { get; set; }
    }
}
