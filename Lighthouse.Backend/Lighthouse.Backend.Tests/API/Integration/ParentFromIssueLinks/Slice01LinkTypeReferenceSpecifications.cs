using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.ParentFromIssueLinks
{
    /// <summary>
    /// Step definitions for the first slice. The instance every scenario starts from carries two link
    /// types and one custom field, which is the smallest description that can tell apart a reference the
    /// field list resolves, one only the link types resolve, and one nothing resolves. The names below are
    /// the vocabulary the scenarios read in.
    /// </summary>
    public partial class Slice01LinkTypeReferenceTest : ParentFromIssueLinksAcceptanceTest
    {
        private const string ALinkType = "Caused by";

        private const string TheLabelALinkTypeCarriesInward = "was caused by";

        private const string TheLabelALinkTypeCarriesOutward = "causes";

        /// <summary>
        /// A live instance ships "Relates" reading "relates to" in both directions, so a reference typed
        /// that way reaches this one type twice over.
        /// </summary>
        private const string AnotherLinkType = "Relates";

        private const string TheLabelAnotherLinkTypeUsesBothWays = "relates to";

        private const string ACustomField = "Story Points";

        private const string ANameTheInstanceUsesForBoth = "Blocks";

        /// <summary>
        /// Each of a link type's three labels can be renamed on its own, so nothing stops an instance
        /// from ending up with one phrase reading against two different types.
        /// </summary>
        private const string ALabelTwoTypesAnswerTo = "duplicates";

        /// <summary>A near miss for <see cref="ALinkType"/> - a name no instance carries, spelled the way a typo arrives.</summary>
        private const string ATypoForALinkType = "Csued by";

        private const string AReferenceTypedAsNothingAtAll = "";

        private const string ATypeCarryingNoDirectionalLabels = "Cloners";

        private const string ATypeDefinedAfterTheOnesMissingTheirNames = "Duplicate";

        /// <summary>
        /// The three ways Jira can answer this endpoint without refusing and without Lighthouse being able
        /// to read the reply: the envelope it parses is keyed issueLinkTypes, and the reply carries that key
        /// under another name, carries no envelope at all, or is not JSON because something in front of Jira
        /// answered instead. Jira Data Center's real answer has never been recorded, and the field list
        /// already differs between the two deployments, so this is the likeliest way the feature meets it.
        /// </summary>
        private const string AnEnvelopeThatDoesNotCarryTheList = "{\"values\":[{\"id\":\"10000\",\"name\":\"Caused by\"}],\"isLast\":true}";

        private const string ABareArrayOfLinkTypes = "[{\"id\":\"10000\",\"name\":\"Caused by\",\"inward\":\"was caused by\",\"outward\":\"causes\"}]";

        private const string AnAnswerThatIsNotJsonAtAll = "<html><body>502 Bad Gateway</body></html>";

        [SetUp]
        public void DescribeTheInstanceEveryScenarioStartsFrom()
        {
            TheInstanceDefinesTheLinkType(ALinkType, TheLabelALinkTypeCarriesInward, TheLabelALinkTypeCarriesOutward);
            TheInstanceDefinesTheLinkType(AnotherLinkType, TheLabelAnotherLinkTypeUsesBothWays, TheLabelAnotherLinkTypeUsesBothWays);
            TheInstanceDefinesTheCustomField(ACustomField);
        }
    }
}
