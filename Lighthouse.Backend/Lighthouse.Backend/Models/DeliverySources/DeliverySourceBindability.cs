namespace Lighthouse.Backend.Models.DeliverySources
{
    /// <summary>
    /// The single bindability rule, shared by the options endpoint and the create path so the picker and
    /// the server can never disagree about what may be bound.
    /// </summary>
    public static class DeliverySourceBindability
    {
        public static SourceOptionBlockReason? For(bool hasDate, bool isRetiredAtSource)
        {
            if (isRetiredAtSource)
            {
                return SourceOptionBlockReason.RetiredAtSource;
            }

            return hasDate ? null : SourceOptionBlockReason.NoDateSet;
        }

        public static bool IsSelectable(bool hasDate, bool isRetiredAtSource)
            => For(hasDate, isRetiredAtSource) is null;
    }
}
