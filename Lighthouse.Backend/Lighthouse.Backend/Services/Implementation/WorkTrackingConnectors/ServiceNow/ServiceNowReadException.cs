using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// Raised when a team's work could not be read from ServiceNow. Every failure on the read path
    /// throws this rather than returning fewer records: <c>WorkItemService.RefreshWorkItems</c>
    /// deletes every stored item the sync did not bring back, so a read that answers a denial with
    /// an empty list destroys the team's SyncedTransitions and CurrentStateEnteredAt, and restoring
    /// the credential does not restore that history.
    /// </summary>
    public class ServiceNowReadException : Exception
    {
        private ServiceNowReadException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        /// <summary>Wraps a rung of the slice-01 verdict ladder, so a refused read keeps its name.</summary>
        public ServiceNowReadException(ConnectionValidationResult verdict)
            : this(verdict.Code, verdict.Message)
        {
        }

        /// <summary>The verdict code, so a caller can tell a denial from a paging fault.</summary>
        public string Code { get; }

        /// <summary>
        /// The instance answered a page with rows it had already sent. That is what an instance
        /// which ignores <c>sysparm_offset</c> looks like, and reading on would either duplicate the
        /// team's work or never stop.
        /// </summary>
        public static ServiceNowReadException RepeatedAPage(string table)
        {
            return new ServiceNowReadException(
                "paging_repeated_records",
                $"ServiceNow returned records from '{table}' that it had already returned on an earlier page. The instance is not honouring sysparm_offset, so Lighthouse stopped rather than counting the same work twice.");
        }

        /// <summary>
        /// The instance kept offering another page well past the size of the result set it reported.
        /// </summary>
        public static ServiceNowReadException PagingDidNotTerminate(string table, int pagesRead, int recordsRead)
        {
            return new ServiceNowReadException(
                "paging_did_not_terminate",
                $"Reading '{table}' did not finish after {pagesRead} pages and {recordsRead} records, which is further than the result set the instance reported. Lighthouse stopped rather than reading without end.");
        }

        /// <summary>
        /// The configured instance address stopped being usable partway through a read. Slice 01
        /// reports this as a verdict on the settings page; on the read path it is a failure, because
        /// the alternative is handing back the pages that happened to succeed.
        /// </summary>
        public static ServiceNowReadException InvalidInstanceAddress(string instanceUrl)
        {
            return new ServiceNowReadException(ServiceNowValidationVerdict.FromInvalidInstanceAddress(instanceUrl));
        }
    }
}
