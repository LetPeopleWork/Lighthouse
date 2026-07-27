namespace Lighthouse.Backend.Services.Implementation
{
    public record BaselineValidationResult(bool IsValid, string ErrorMessage = "");

    public static class BaselineValidationService
    {
        private const int MinimumBaselineDays = 14;

        public static BaselineValidationResult Validate(DateTime? startDate, DateTime? endDate, int doneItemsCutoffDays, DateOnly today)
        {
            if (startDate == null && endDate == null)
            {
                return new BaselineValidationResult(true);
            }

            if (startDate == null || endDate == null)
            {
                return new BaselineValidationResult(false, "Baseline start and end dates must both be set or both be empty.");
            }

            if (endDate.Value.Date <= startDate.Value.Date)
            {
                return new BaselineValidationResult(false, "Baseline end date must be after the start date.");
            }

            var baselineDays = (endDate.Value.Date - startDate.Value.Date).Days;
            if (baselineDays < MinimumBaselineDays)
            {
                return new BaselineValidationResult(false, $"Baseline must be at least {MinimumBaselineDays} days. Current: {baselineDays} days.");
            }

            if (DateOnly.FromDateTime(endDate.Value) > today)
            {
                return new BaselineValidationResult(false, "Baseline end date must not be in the future.");
            }

            if (doneItemsCutoffDays <= 0)
            {
                // CutOff = 0 means full history - no need to check
                return new BaselineValidationResult(true);
            }

            var cutoffDate = today.AddDays(-doneItemsCutoffDays);
            if (DateOnly.FromDateTime(startDate.Value) < cutoffDate)
            {
                return new BaselineValidationResult(false, $"Baseline start date falls outside the data cutoff window ({doneItemsCutoffDays} days). Data may be incomplete.");
            }

            return new BaselineValidationResult(true);
        }
    }
}
