using System.Text.RegularExpressions;
using Lighthouse.Backend.Tests.API.Integration.FastWithManyFeatures;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The round rules read the source text because the calls they forbid sit inside async methods, where a
    /// compiled-type check sees them in a Debug build and loses them in a Release one. The endpoint rule reads
    /// the constructor, which both builds keep.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-6400-fast-with-many-features")]
    public partial class FastWithManyFeaturesArchUnitTest
    {
        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string TheForecastService = "Services/Implementation/Forecast/ForecastService.cs";

        private const string TheForecastUpdater = "Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs";

        private const string TheCompositionRoot = "Program.cs";

        private const string TheLaneFileName = "ForecastLane.cs";

        private const string TheListController = "Lighthouse.Backend.API.FeatureListController";

        private static readonly string[] ExcludedPathSegments = ["/obj/", "/bin/", "/StrykerOutput"];

        [Test]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("slice-01")]
        [Category("story-6404")]
        [Category("kpi-OUT-6400-no-stall-during-rounds")]
        public void The_forecast_service_never_hands_a_simulation_to_the_shared_pool()
        {
            var code = TheCodeOf(TheForecastService);

            Assert.That(TheCallsThatLandOnTheSharedPool(code), Is.Empty,
                "ForecastService hands work to the threads that answer pages. A background round must simulate on the " +
                "scheduler it was started on, so every start and every parallel loop names that scheduler.");
        }

        [Test]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("slice-01")]
        [Category("story-6404")]
        [Category("kpi-OUT-6400-no-stall-during-rounds")]
        public void Forecast_round_code_never_resumes_off_the_forecast_threads()
        {
            var lane = TheLaneFile();

            var offenders = new[] { TheForecastService, TheForecastUpdater, lane }
                .OfType<string>()
                .Where(path => ConfigureAwaitFalse().IsMatch(TheCodeOf(path)))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lane, Is.Not.Null, "There are no forecast threads yet: no ForecastLane.cs in the production project.");
                Assert.That(offenders, Is.Empty,
                    "ConfigureAwait(false) in round code lets the round carry on on the threads that answer pages. Found in: " + string.Join(", ", offenders));
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice01)]
        [Category("slice-01")]
        [Category("story-6404")]
        public void Only_the_forecast_updater_and_the_composition_root_reach_the_forecast_threads()
        {
            var lane = TheLaneFile();
            var allowed = new HashSet<string>(StringComparer.Ordinal) { TheForecastUpdater, TheCompositionRoot };
            if (lane is not null)
            {
                allowed.Add(lane);
            }

            var others = EveryProductionSourceFile()
                .Where(path => !allowed.Contains(path) && LaneReference().IsMatch(TheCodeOf(path)))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lane, Is.Not.Null, "There are no forecast threads yet: no ForecastLane.cs in the production project.");
                Assert.That(others, Is.Empty,
                    "Only the forecast updater hands a round to the forecast threads, and only the composition root creates them. Also reached from: " + string.Join(", ", others));
            }
        }

        [Test]
        [Ignore(PendingEpic6400.Slice04)]
        [Category("slice-04")]
        [Category("story-6408")]
        public void The_Features_list_endpoint_reads_through_its_service_and_never_from_a_repository()
        {
            var controller = typeof(Backend.Program).Assembly.GetType(TheListController);

            Assert.That(controller, Is.Not.Null, $"There is no {TheListController} yet.");

            var repositories = controller!.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .Select(parameter => parameter.ParameterType)
                .Where(type => type.Name.EndsWith("Repository", StringComparison.Ordinal) || type.Name.StartsWith("IRepository`", StringComparison.Ordinal))
                .Select(type => type.Name)
                .ToList();

            Assert.That(repositories, Is.Empty,
                "The Features list endpoint must read through its service, not take a repository: " + string.Join(", ", repositories));
        }

        // Task.Run and the thread pool always mean the shared pool. A StartNew or a parallel loop does too, unless it
        // is told to run on the scheduler it was started on or on the forecast lane's; TaskScheduler.Default is the
        // shared pool under another name, so naming it lets nothing through.
        private static List<string> TheCallsThatLandOnTheSharedPool(string code)
        {
            var optionsThatNameAScheduler = OptionsDeclaration().Matches(code)
                .Where(declaration => NamesTheRoundsScheduler(declaration.Groups["body"].Value))
                .Select(declaration => declaration.Groups["name"].Value)
                .ToHashSet(StringComparer.Ordinal);

            return [.. WorkHandedToAThread().Matches(code)
                .Select(call => TheStatementAt(code, call.Index))
                .Where(statement => !(StartNewCall().IsMatch(statement) && NamesTheRoundsScheduler(statement)))
                .Where(statement => !(ParallelLoop().IsMatch(statement) && TheLoopGetsOptionsThatNameAScheduler(statement, optionsThatNameAScheduler)))];
        }

        // Each loop is judged by the options it is handed, either declared inline or as a variable declared with
        // them, so one well-configured loop cannot vouch for another in the same file.
        private static bool TheLoopGetsOptionsThatNameAScheduler(string loop, HashSet<string> optionsThatNameAScheduler)
            => (InlineOptions().IsMatch(loop) && NamesTheRoundsScheduler(loop))
                || Identifier().Matches(loop).Any(name => optionsThatNameAScheduler.Contains(name.Value));

        private static bool NamesTheRoundsScheduler(string code)
            => RoundsScheduler().IsMatch(code) && !DefaultScheduler().IsMatch(code);

        private static string TheStatementAt(string code, int start)
        {
            var end = code.IndexOf(';', start);
            return code[start..(end < 0 ? code.Length : end)];
        }

        [GeneratedRegex(@"\bTask\s*\.\s*(Run|Factory\s*\.\s*StartNew)\b|\bThreadPool\s*\.|\bParallel\s*\.")]
        private static partial Regex WorkHandedToAThread();

        [GeneratedRegex(@"^Task\s*\.\s*Factory\s*\.\s*StartNew\b")]
        private static partial Regex StartNewCall();

        [GeneratedRegex(@"^Parallel\s*\.")]
        private static partial Regex ParallelLoop();

        [GeneratedRegex(@"\b(?:var|ParallelOptions)\s+(?<name>\w+)\s*=\s*new\s*(?:ParallelOptions)?\s*(?:\(\s*\))?\s*\{(?<body>[^}]*)\}")]
        private static partial Regex OptionsDeclaration();

        [GeneratedRegex(@"\bnew\s+ParallelOptions\b")]
        private static partial Regex InlineOptions();

        [GeneratedRegex(@"\b[A-Za-z_]\w*\b")]
        private static partial Regex Identifier();

        [GeneratedRegex(@"\bTaskScheduler\s*\.\s*Current\b|\b\w*[Ll]ane\w*\b")]
        private static partial Regex RoundsScheduler();

        [GeneratedRegex(@"\bTaskScheduler\s*\.\s*Default\b")]
        private static partial Regex DefaultScheduler();

        [GeneratedRegex(@"\.ConfigureAwait\s*\(\s*false\s*\)")]
        private static partial Regex ConfigureAwaitFalse();

        [GeneratedRegex(@"\bForecastLane\b")]
        private static partial Regex LaneReference();

        [GeneratedRegex(@"//[^\n]*|/\*.*?\*/", RegexOptions.Singleline)]
        private static partial Regex Comments();

        private static string? TheLaneFile()
            => EveryProductionSourceFile().FirstOrDefault(path => Path.GetFileName(path) == TheLaneFileName);

        private static string TheCodeOf(string relativePath)
            => Comments().Replace(File.ReadAllText(Path.Combine(ProductionRoot(), relativePath)), string.Empty);

        private static List<string> EveryProductionSourceFile()
        {
            var root = ProductionRoot();

            return [.. Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
                .Where(path => !ExcludedPathSegments.Any(segment => ("/" + path).Contains(segment, StringComparison.Ordinal)))];
        }

        private static string ProductionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Lighthouse.sln could not be found to anchor the source scan.");

            return Path.Combine(directory!.FullName, ProductionProjectDirectory);
        }
    }
}
