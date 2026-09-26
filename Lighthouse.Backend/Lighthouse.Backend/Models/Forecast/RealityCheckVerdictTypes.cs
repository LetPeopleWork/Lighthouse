namespace Lighthouse.Backend.Models.Forecast
{
    public enum Determination
    {
        AllWindowsAlike,
        SomeWindowsSound,
        NoWindowSound,
        NotEnoughEvidence,
    }

    public enum CurrentSettingStanding
    {
        Inside,
        Outside,
        NotDetermined,
        NotTested,
    }

    public enum NotTestedReason
    {
        UsesFixedDates,
        NotAPositiveLength,
    }

    public enum SufficiencyReason
    {
        Sufficient,
        TooFewActiveDays,
        DegenerateForecast,
    }

    public enum CellOutcome
    {
        OverForecast,
        WithinBand,
        UnderForecast,
    }

    public enum LevelReading
    {
        SometimesHeld,
        NeverHeld,
        AlwaysHeld,
        NotEvaluated,
    }
}
