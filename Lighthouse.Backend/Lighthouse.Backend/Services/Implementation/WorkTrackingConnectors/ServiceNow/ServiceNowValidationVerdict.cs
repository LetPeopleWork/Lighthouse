using System.Net;
using Lighthouse.Backend.Models.Validation;

namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    // ADR-114. The functional core of the connection probe: ServiceNow answers a denial with a
    // success (200 + zero rows), so the verdict is the only interesting logic in the slice and it
    // is kept pure — no IO, no logging, no persistence — so every rung of the ladder is reachable
    // as a table-driven unit test. ServiceNowValidationVerdictPurityArchUnitTest enforces that.
    public static class ServiceNowValidationVerdict
    {
        private const string UnexpectedResponseCode = "unexpected_response";

        /// <summary>
        /// Rung 0 — the configured instance address is not an absolute URL. Pre-flight, no IO.
        /// </summary>
        public static ConnectionValidationResult FromInvalidInstanceAddress(string instanceUrl)
        {
            return ConnectionValidationResult.Failure(
                "invalid_url",
                $"'{instanceUrl}' is not a valid ServiceNow instance address. Enter the full address of the instance, including the scheme.",
                $"The configured address could not be parsed as an absolute URI: '{instanceUrl}'.");
        }

        /// <summary>
        /// Rung 1 — the instance could not be reached at all (DNS, refused, TLS, timeout).
        /// </summary>
        public static ConnectionValidationResult FromUnreachableInstance(string technicalDetails)
        {
            return ConnectionValidationResult.Failure(
                "connection_failed",
                "The ServiceNow instance could not be reached. Check the instance address, and that this Lighthouse installation can reach it over the network.",
                technicalDetails);
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
            return statusCode switch
            {
                HttpStatusCode.Unauthorized => RejectedCredential(),
                HttpStatusCode.OK when !responseIsJson => NotData(),
                HttpStatusCode.BadRequest => UnknownTable(table),
                HttpStatusCode.Forbidden => ReadRefused(table),
                HttpStatusCode.OK when rowCount < 1 => NothingVisible(table),
                HttpStatusCode.OK => ConnectionValidationResult.Success(),
                _ => UnrecognisedAnswer(statusCode),
            };
        }

        private static ConnectionValidationResult RejectedCredential()
        {
            // ADR-115 keeps the role hint conditional: the restriction properties are measurably
            // invisible to the least-privilege account that would need them, so Lighthouse must
            // not assert the restriction is the cause. Detection is forbidden, not merely absent.
            return ConnectionValidationResult.Failure(
                "authentication_failed",
                "ServiceNow rejected the credential. Check the user name and password, and that the account is active.",
                "ServiceNow returned 401. If this instance enforces the inbound basic-auth restriction, the account also needs the snc_basic_auth_api_access role — Lighthouse cannot check this for you.");
        }

        private static ConnectionValidationResult NotData()
        {
            return ConnectionValidationResult.Failure(
                UnexpectedResponseCode,
                "The instance answered successfully but did not return data, which usually means a sign-in page was served instead. Lighthouse needs an account that can authenticate against the Table API with a user name and password rather than through single sign-on.",
                "The response carried a success status but its body was not JSON.");
        }

        private static ConnectionValidationResult UnknownTable(string table)
        {
            return ConnectionValidationResult.Failure(
                "unknown_table",
                $"ServiceNow does not recognise the table '{table}'. Check the spelling, and use the table's system name rather than the label shown in the interface.",
                $"ServiceNow returned 400 for the table '{table}'.");
        }

        private static ConnectionValidationResult ReadRefused(string table)
        {
            return ConnectionValidationResult.Failure(
                "insufficient_permissions",
                $"ServiceNow refused to read the table '{table}' with this account. Grant the account a role that can read that table.",
                $"ServiceNow returned 403 for the table '{table}'.");
        }

        private static ConnectionValidationResult NothingVisible(string table)
        {
            // ADR-114 decision 4 / contradiction C-1. Zero visible rows is byte-identical for an
            // unauthorised read and a genuinely empty table, and every discriminator the SPIKE
            // tried is itself denied to the account that needs it — so the message names both
            // causes rather than asserting a certainty the platform cannot supply.
            return ConnectionValidationResult.Failure(
                "no_records_visible",
                $"The credential authenticated, but the table '{table}' returned no visible rows. Either the account lacks read access to it — grant sn_incident_read or the matching per-table role; note that snc_read_only grants no read access at all despite its name — or the table is genuinely empty.",
                $"ServiceNow returned 200 with zero rows for the table '{table}'.");
        }

        private static ConnectionValidationResult UnrecognisedAnswer(HttpStatusCode statusCode)
        {
            return ConnectionValidationResult.Failure(
                UnexpectedResponseCode,
                $"ServiceNow answered with an unexpected status ({(int)statusCode}). Check that the configured address points at a ServiceNow instance and that the instance is healthy.",
                $"Unhandled status code {(int)statusCode} ({statusCode}).");
        }
    }
}
