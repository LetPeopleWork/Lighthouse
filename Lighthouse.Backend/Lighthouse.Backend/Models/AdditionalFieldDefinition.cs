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

        /// <summary>What a field reads as wherever it is named. An id no definition answers to still reads as something that can be told apart and fixed.</summary>
        public static string NameShownIn(WorkTrackingSystemConnection? connection, int? id)
            => connection?.DisplayNameOfField(id) ?? $"Unknown field (id {id})";
    }
}
