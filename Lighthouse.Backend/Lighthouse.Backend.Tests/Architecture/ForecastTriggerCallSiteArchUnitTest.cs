namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A coalesced forecast request waits for every refresh that feeds the portfolio, and it does not
    /// leave itself out of that set. That is what makes one forecast per overlapping group a structural
    /// outcome rather than a race: whoever asks first holds, its own run has not ended, so the hold cannot
    /// have cleared by the time anybody else asks, and every later asker finds the forecast promised.
    ///
    /// The whole guarantee rests on one precondition - that every caller of the coalesced entry point asks
    /// from inside a refresh whose own key is in the wait set. A caller from anywhere else has nothing to
    /// hold behind, gets no protection from the first-asker rule, and brings back the double forecast an
    /// operator reads as a delivery date that settles and then moves. Two files may ask, and a third has to
    /// be a decision somebody makes rather than a line somebody adds.
    ///
    /// A source scanner rather than an ArchUnitNET dependency rule, in the shape of
    /// <c>UpdateCancellationContextWriterArchUnitTest</c>: the rule is about which method is called, not
    /// about which types depend on which. <c>ForecastController</c> and both rank handlers legitimately
    /// depend on the same port for the hand-triggered entry point, which deliberately bypasses all of this
    /// because a person pressed a button and is watching for the answer.
    /// </summary>
    [TestFixture]
    public class ForecastTriggerCallSiteArchUnitTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string ForecastPortTypeName = "IForecastUpdater";

        private const string CoalescedTrigger = "TriggerUpdate(";

        /// <summary>Asks from inside the portfolio's own Features refresh, whose key is in the wait set.</summary>
        private const string PortfolioUpdaterRelativePath = "Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs";

        /// <summary>Asks from inside the team's refresh, whose key is in the wait set.</summary>
        private const string TeamTriggerHandlerRelativePath = "Services/Implementation/BackgroundServices/Update/TeamDataRefreshedForecastTriggerHandler.cs";

        /// <summary>Declares the thing being guarded; its own <c>base.TriggerUpdate</c> is the admission, not a request.</summary>
        private const string ForecastUpdaterRelativePath = "Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs";

        [Test]
        public void OnlyARefreshThatTheForecastWaitsFor_AsksForACoalescedForecast()
        {
            var productionRoot = Path.Combine(RepositoryRoot(), ProductionProjectDirectory);
            var allowed = new[] { PortfolioUpdaterRelativePath, TeamTriggerHandlerRelativePath, ForecastUpdaterRelativePath }
                .Select(relative => Path.Combine(productionRoot, relative.Replace('/', Path.DirectorySeparatorChar)))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var offenders = new List<string>();

            foreach (var file in Directory.EnumerateFiles(productionRoot, "*.cs", SearchOption.AllDirectories))
            {
                if (allowed.Contains(file) || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);

                // A file can only reach the coalesced trigger if it names the port somewhere - to declare a
                // field, take it as a parameter, or resolve it. The call line itself says only
                // "something.TriggerUpdate(id)", which every updater in the codebase also says about its own
                // work, so the port has to be looked for across the file rather than on the line.
                if (!lines.Any(line => line.Contains(ForecastPortTypeName, StringComparison.Ordinal)))
                {
                    continue;
                }

                for (var line = 0; line < lines.Length; line++)
                {
                    if (AsksForACoalescedForecast(lines[line]))
                    {
                        offenders.Add($"{Path.GetRelativePath(productionRoot, file)}:{line + 1}  {lines[line].Trim()}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "Asking for a coalesced forecast from outside a refresh the forecast waits for produces a second "
                + "forecast for the same portfolio, and the operator watches the delivery date settle and then "
                + "move. Either ask from inside such a refresh, or use the hand-triggered entry point.\n"
                + string.Join("\n", offenders));
        }

        /// <summary>
        /// A call to the coalesced trigger, however the port instance is named. The preceding dot is what
        /// makes it a call on something rather than a declaration or a prose mention, and a comment
        /// describing the rule is not a breach of it.
        /// </summary>
        private static bool AsksForACoalescedForecast(string line)
        {
            var code = line.TrimStart();
            if (code.StartsWith("//", StringComparison.Ordinal))
            {
                return false;
            }

            var call = code.IndexOf(CoalescedTrigger, StringComparison.Ordinal);

            return call > 0 && code[call - 1] == '.';
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the forecast-trigger scan.");
            return directory!.FullName;
        }
    }
}
