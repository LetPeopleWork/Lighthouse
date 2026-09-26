namespace Lighthouse.Backend.Models.Forecast
{
    public sealed record RealityCheckResultDto(
        int TeamId,
        string TeamName,
        DateOnly AnchorDate,
        IReadOnlyList<int> StandardWindowDays,
        IReadOnlyList<int> SampledWindowDays,
        IReadOnlyList<int> SampledHorizonDays,
        IReadOnlyList<int> ConfidenceLevels,
        int MinimumActiveDays,
        RealityCheckDenominatorDto Denominator,
        RealityCheckSoundWindowDto SoundWindow,
        IReadOnlyList<RealityCheckLevelCoverageDto> LevelCoverage,
        IReadOnlyList<RealityCheckCellDto> Cells)
    {
        public bool FilterApplied { get; init; }

        public string? ExcludedSummary { get; init; }
    }

    public sealed record RealityCheckDenominatorDto(int RunsAttempted, int RunsEvaluated, int LevelsPerRun, int ScoresEvaluated);

    public sealed record RealityCheckSoundWindowDto(
        IReadOnlyList<int> SoundWindowDays,
        IReadOnlyList<int> UnevaluatedWindowDays,
        Determination Determination,
        int CurrentSettingDays,
        bool CurrentSettingWasTested,
        CurrentSettingStanding CurrentSettingStanding,
        NotTestedReason? CurrentSettingNotTestedReason);

    public sealed record RealityCheckLevelCoverageDto(int ConfidenceLevel, int HeldCount, double ExpectedHeldCount, LevelReading Reading);

    /// <summary>
    /// One check: a sampling window's history forecast over one horizon and scored against what the Team finished.
    /// Every date names a calendar day inside the stretch it bounds, first and last alike. The scored period holds
    /// exactly <c>HorizonDays</c> days and ends on the anchor day; the history holds exactly <c>SamplingWindowDays</c>
    /// days and ends the day before the scored period starts, so no check learns from a day it is scored on.
    /// </summary>
    public sealed record RealityCheckCellDto(
        int HorizonDays,
        int SamplingWindowDays,
        DateOnly ScoredPeriodStart,
        DateOnly ScoredPeriodEnd,
        DateOnly HistoryWindowStart,
        DateOnly HistoryWindowEnd,
        RealityCheckSufficiencyDto Sufficiency,
        IReadOnlyList<RealityCheckForecastDto>? Forecast,
        int? ActualCompleted,
        CellOutcome? Outcome,
        IReadOnlyList<RealityCheckLevelOutcomeDto>? LevelOutcomes);

    public sealed record RealityCheckSufficiencyDto(bool IsSufficient, SufficiencyReason Reason, int DaysWithCompletedWork);

    public sealed record RealityCheckForecastDto(int Probability, int Value);

    public sealed record RealityCheckLevelOutcomeDto(int ConfidenceLevel, int ForecastValue, bool Held);
}
