namespace Lighthouse.Backend.API.DTO.Reports
{
    /// <summary>The reports to remove, by id alone: the server finds each one's owner itself.</summary>
    public sealed class ReportRemovalRequest
    {
        public List<int>? ReportIds { get; set; }
    }
}
