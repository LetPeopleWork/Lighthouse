using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.Validation
{
    public class ConnectionValidationResult
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("technicalDetails")]
        public string? TechnicalDetails { get; set; }

        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }

        /// <summary>
        /// Something the connection works fine without, but that the administrator should know about
        /// — a capability this instance cannot offer, and what it would take to offer it.
        /// </summary>
        /// <remarks>
        /// ADR-118 decision 5. A validation that succeeds still has things worth saying: ServiceNow
        /// without <c>itil</c> reports request-to-resolution rather than time-in-progress, and the
        /// administrator who configured the connection is the person who can change that. The
        /// advisory rides the success rather than a chart annotation, because a caveat pinned to
        /// every chart is noise while this is a configuration fact. Re-validating re-evaluates it,
        /// so granting the role later clears it.
        /// </remarks>
        [JsonPropertyName("advisory")]
        public string? Advisory { get; set; }

        /// <summary>The machine-readable half of <see cref="Advisory"/>, free-form per connector.</summary>
        [JsonPropertyName("advisoryCode")]
        public string? AdvisoryCode { get; set; }

        public static ConnectionValidationResult Success()
        {
            return new ConnectionValidationResult
            {
                IsValid = true,
                Code = "valid",
                Message = "Connection validated successfully."
            };
        }

        /// <summary>
        /// A working connection carrying a capability limitation worth reporting (ADR-118 D5).
        /// </summary>
        public static ConnectionValidationResult SuccessWith(string advisoryCode, string advisory)
        {
            var result = Success();
            result.AdvisoryCode = advisoryCode;
            result.Advisory = advisory;

            return result;
        }

        public static ConnectionValidationResult Failure(
            string code,
            string message,
            string? technicalDetails = null,
            string? fieldName = null)
        {
            return new ConnectionValidationResult
            {
                IsValid = false,
                Code = code,
                Message = message,
                TechnicalDetails = technicalDetails,
                FieldName = fieldName,
            };
        }
    }
}