namespace Lighthouse.Backend.API.DTO
{
    public sealed record FlowEfficiencyInfoDto(
        bool IsConfigured,
        bool HasDataInScope,
        double EfficiencyPercent,
        double TotalDoingDays,
        double WaitDays);
}
