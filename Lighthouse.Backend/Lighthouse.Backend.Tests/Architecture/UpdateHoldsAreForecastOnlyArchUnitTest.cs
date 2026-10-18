using System.Text.RegularExpressions;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Interfaces.Update;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Cancel all drops every update that is held until other work clears, and leaves the write-back round
    /// that held it to finish. That is only safe while the forecast is the one thing ever held: a held Team
    /// or Portfolio refresh dropped that way would vanish without its round being told. So only the forecast
    /// updater may ask for a hold.
    /// </summary>
    [TestFixture]
    [Category("epic-6311-responsive-during-updates")]
    [Category("slice-05")]
    [Category("ac-05.2")]
    public class UpdateHoldsAreForecastOnlyArchUnitTest
    {
        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        private const string HoldName = nameof(IUpdateQueueService.HoldUntilQueuedWorkClears);

        /// <summary>Already true, and kept as the guard Cancel all relies on.</summary>
        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void Nothing_but_the_forecast_updater_holds_an_update_until_other_work_clears()
        {
            Types().That()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ForecastUpdater))).And()
                .DoNotHaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(UpdateQueueService)))
                .Should().NotCallAny(MethodMembers().That().HaveNameStartingWith(HoldName))
                .Because("Cancel all drops held updates, which is safe only because every one of them is a forecast")
                .Check(Architecture);
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public void The_forecast_updater_is_seen_to_hold_its_forecast()
        {
            Types().That()
                .HaveFullNameMatching(TheTypeOrOneNestedInIt(typeof(ForecastUpdater)))
                .Should().CallAny(MethodMembers().That().HaveNameStartingWith(HoldName))
                .Because("the rule above only means something while it can see the one caller that does hold")
                .Check(Architecture);
        }

        private static string TheTypeOrOneNestedInIt(Type type)
            => "^" + Regex.Escape(type.FullName!) + @"($|\+)";
    }
}
