using System.Security.Claims;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    public static class GroupClaimParser
    {
        public static bool TryGetGroupValues(
            ClaimsPrincipal principal,
            string claimName,
            out HashSet<string> groupValues,
            out bool hasUnsupportedFormat)
        {
            groupValues = [];
            hasUnsupportedFormat = false;

            var claims = principal.FindAll(claimName);
            foreach (var claim in claims)
            {
                var claimValue = claim.Value.Trim();
                if (string.IsNullOrWhiteSpace(claimValue))
                {
                    continue;
                }

                if (claimValue.StartsWith('['))
                {
                    if (!TryParseJsonArrayClaim(claimValue, out var parsedValues))
                    {
                        hasUnsupportedFormat = true;
                        return true;
                    }

                    foreach (var parsedValue in parsedValues)
                    {
                        groupValues.Add(parsedValue);
                    }

                    continue;
                }

                if (claimValue.StartsWith('{'))
                {
                    hasUnsupportedFormat = true;
                    return true;
                }

                groupValues.Add(claimValue);
            }

            return true;
        }

        private static bool TryParseJsonArrayClaim(string claimValue, out IReadOnlyList<string> values)
        {
            values = [];

            try
            {
                using var document = JsonDocument.Parse(claimValue);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return false;
                }

                var parsedValues = new List<string>();
                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    var value = element.GetString();
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        continue;
                    }

                    parsedValues.Add(value.Trim());
                }

                values = parsedValues;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
