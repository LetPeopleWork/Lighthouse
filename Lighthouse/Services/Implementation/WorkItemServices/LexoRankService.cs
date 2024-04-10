namespace Lighthouse.Services.Implementation.WorkItemServices
{
    public class LexoRankService : ILexoRankService
    {
        public string Default => "00000|";

        public string GetHigherPriority(string currentRank)
        {
            // Increment the last character by 1 to get a higher priority
            char[] rankChars = currentRank.ToCharArray();
            rankChars[currentRank.Length - 2]++; // Assuming the last character represents the priority level
            return new string(rankChars);
        }

        public string GetLowerPriority(string currentRank)
        {
            // Decrement the last character by 1 to get a lower priority
            char[] rankChars = currentRank.ToCharArray();
            rankChars[currentRank.Length - 2]--; // Assuming the last character represents the priority level
            return new string(rankChars);
        }
    }
}
