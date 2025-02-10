namespace Lighthouse.Backend.Models.History
{
    public class FeatureWorkHistoryEntry
    {
        public FeatureWorkHistoryEntry()
        {
        }

        public FeatureWorkHistoryEntry(FeatureWork featureWork, FeatureHistoryEntry featureHistoryEntry)
        {
            TeamId = featureWork.TeamId;
            RemainingWorkItems = featureWork.RemainingWorkItems;
            TotalWorkItems = featureWork.TotalWorkItems;

            FeatureHistoryEntryId = featureHistoryEntry.Id;
            FeatureHistoryEntry = featureHistoryEntry;            
        }

        public int Id { get; set; }

        public int TeamId { get; set; }

        public int RemainingWorkItems { get; set; }

        public int TotalWorkItems { get; set; }

        public int FeatureHistoryEntryId { get; set; }

        public FeatureHistoryEntry FeatureHistoryEntry { get; set; }
    }
}
