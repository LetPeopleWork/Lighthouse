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

        public static ConnectionValidationResult Success()
        {
            return new ConnectionValidationResult
            {
                IsValid = true,
                Code = "valid",
                Message = "Connection validated successfully."
            };
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