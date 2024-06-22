namespace Lighthouse.Services.Implementation.WorkItemServices
{
    public class LexoRankService : ILexoRankService
    {
        private readonly ILogger<LexoRankService> logger;

        public LexoRankService(ILogger<LexoRankService> logger)
        {
            this.logger = logger;
        }

        public string Default => "00000|";

        public string GetHigherPriority(string currentRank)
        {
            logger.LogDebug("Trying to find higher rank than {currentRank}", currentRank);

            // Increment the last character by 1 to get a higher priority
            char[] rankChars = currentRank.ToCharArray();
            rankChars[currentRank.Length - 2]++; // Assuming the last character represents the priority level

            var newRank = new string(rankChars);

            logger.LogDebug("Generated Rank: {newRank}", newRank);

            return newRank;
        }

        public string GetLowerPriority(string currentRank)
        {
            logger.LogDebug("Trying to find lower rank than {currentRank}", currentRank);

            // Decrement the last character by 1 to get a lower priority
            char[] rankChars = currentRank.ToCharArray();
            rankChars[currentRank.Length - 2]--; // Assuming the last character represents the priority level

            var newRank = new string(rankChars);

            logger.LogDebug("Generated Rank: {newRank}", newRank);

            return newRank;
        }
    }
}
