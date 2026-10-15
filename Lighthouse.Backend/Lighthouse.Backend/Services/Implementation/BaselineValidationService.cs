namespace Lighthouse.Backend.Services.Implementation
{
    public record BaselineValidationResult(bool IsValid, string ErrorMessage = "");

    public static class BaselineValidationService
    {
        private const int MinimumBaselineDays = 14;

        // The browser cannot know the instance's day, and a viewer's today is at most one day ahead
        // of it, so a baseline picked to end on the viewer's today may end one day after the
        // instance's day without being in the future.
        private const int DaysAViewerCanBeAheadOfTheInstance = 1;

        // The baseline bounds are user-picked calendar days stored at UTC midnight, so
        // DateOnly.FromDateTime is correct here and ToInstanceDay would be wrong - it would move a
        // date picked west of UTC back a day.
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

            var lengthResult = ValidateLength(startDate.Value, endDate.Value);
            if (!lengthResult.IsValid)
            {
                return lengthResult;
            }

            if (DateOnly.FromDateTime(endDate.Value) > today.AddDays(DaysAViewerCanBeAheadOfTheInstance))
            {
                return new BaselineValidationResult(false, "Baseline end date must not be in the future.");
            }

            return ValidateCutoff(startDate.Value, doneItemsCutoffDays, today);
        }

        // With no baseline configured, the display window stands in for one. It ends on the viewer's
        // today, which may be ahead of the instance's day, so it is never refused as future.
        public static BaselineValidationResult ValidateDisplayWindowAsBaseline(DateTime startDate, DateTime endDate, int doneItemsCutoffDays, DateOnly today)
        {
            var lengthResult = ValidateLength(startDate, endDate);
            if (!lengthResult.IsValid)
            {
                return lengthResult;
            }

            return ValidateCutoff(startDate, doneItemsCutoffDays, today);
        }

        private static BaselineValidationResult ValidateLength(DateTime startDate, DateTime endDate)
        {
            if (endDate.Date <= startDate.Date)
            {
                return new BaselineValidationResult(false, "Baseline end date must be after the start date.");
            }

            var baselineDays = (endDate.Date - startDate.Date).Days;
            if (baselineDays < MinimumBaselineDays)
            {
                return new BaselineValidationResult(false, $"Baseline must be at least {MinimumBaselineDays} days. Current: {baselineDays} days.");
            }

            return new BaselineValidationResult(true);
        }

        private static BaselineValidationResult ValidateCutoff(DateTime startDate, int doneItemsCutoffDays, DateOnly today)
        {
            if (doneItemsCutoffDays <= 0)
            {
                // CutOff = 0 means full history - no need to check
                return new BaselineValidationResult(true);
            }

            var cutoffDate = today.AddDays(-doneItemsCutoffDays);
            if (DateOnly.FromDateTime(startDate) < cutoffDate)
            {
                return new BaselineValidationResult(false, $"Baseline start date falls outside the data cutoff window ({doneItemsCutoffDays} days). Data may be incomplete.");
            }

            return new BaselineValidationResult(true);
        }
    }
}
