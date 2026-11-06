namespace Lighthouse.Backend.Models
{
    public class AdditionalFieldDefinition
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public bool IsPredefined { get; set; }

        public int WorkTrackingSystemConnectionId { get; set; }

        public WorkTrackingSystemConnection? WorkTrackingSystemConnection { get; set; }

        /// <summary>What an id reads as once no definition answers to it, so it can still be told apart and fixed.</summary>
        public static string NameOfAnUnknownField(int? id) => $"Unknown field (id {id})";
    }
}
