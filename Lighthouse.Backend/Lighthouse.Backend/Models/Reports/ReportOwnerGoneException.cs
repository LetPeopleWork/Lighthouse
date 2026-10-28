namespace Lighthouse.Backend.Models.Reports
{
    /// <summary>The owner a report was being made for no longer exists. Names only the owner's kind and id, never its name.</summary>
    public sealed class ReportOwnerGoneException(ReportOwner owner)
        : InvalidOperationException($"{owner.Kind} {owner.Id} does not exist")
    {
        public ReportOwner Owner { get; } = owner;
    }
}
