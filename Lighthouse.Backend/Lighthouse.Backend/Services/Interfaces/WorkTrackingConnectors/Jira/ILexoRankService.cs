namespace Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira
{
    public interface ILexoRankService
    {
        string Default { get; }

        string GetHigherPriority(string currentRank);

        string GetLowerPriority(string currentRank);
    }
}