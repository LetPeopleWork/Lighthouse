namespace Lighthouse.Services.Implementation.WorkItemServices
{
    public interface ILexoRankService
    {
        string Default { get; }

        string GetHigherPriority(string currentRank);

        string GetLowerPriority(string currentRank);
    }
}