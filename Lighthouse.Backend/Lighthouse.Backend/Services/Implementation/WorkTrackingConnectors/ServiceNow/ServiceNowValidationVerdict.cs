using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // SCAFFOLD (DISTILL slice 01, Story #5574) — signatures only. Every entry point returns the
    // sentinel below, so ServiceNowValidationVerdictTest fails at its assertions
    // (MISSING_FUNCTIONALITY) rather than at a thrown exception. DELIVER replaces the bodies.
    //
    // ADR-114. The functional core of the connection probe: ServiceNow answers a denial with a
    // success (200 + zero rows), so the verdict is the only interesting logic in the slice and it
    // is kept pure — no IO, no logging, no persistence — so every rung of the ladder is reachable
    // as a table-driven unit test. ServiceNowValidationVerdictPurityArchUnitTest enforces that.
    public static class ServiceNowValidationVerdict
    {
        private const string ScaffoldCode = "__scaffold__";

        /// <summary>
        /// Rung 0 — the configured instance address is not an absolute URL. Pre-flight, no IO.
        /// </summary>
        public static ConnectionValidationResult FromInvalidInstanceAddress(string instanceUrl)
        {
            return NotYetImplemented(instanceUrl);
        }

        /// <summary>
        /// Rung 1 — the instance could not be reached at all (DNS, refused, TLS, timeout).
        /// </summary>
        public static ConnectionValidationResult FromUnreachableInstance(string technicalDetails)
        {
            return NotYetImplemented(technicalDetails);
        }

        /// <summary>
        /// Rungs 2-7 — the instance answered. The three scalars are everything the ladder needs:
        /// the status, whether the body was JSON at all, and how many rows came back.
        /// </summary>
        public static ConnectionValidationResult FromResponse(
            HttpStatusCode statusCode,
            bool responseIsJson,
            int rowCount,
            string table)
        {
            return NotYetImplemented($"{statusCode} json={responseIsJson} rows={rowCount} table={table}");
        }

        private static ConnectionValidationResult NotYetImplemented(string observed)
        {
            return ConnectionValidationResult.Failure(
                ScaffoldCode,
                "Not yet implemented - RED scaffold (DISTILL slice 01).",
                observed);
        }
    }
}
