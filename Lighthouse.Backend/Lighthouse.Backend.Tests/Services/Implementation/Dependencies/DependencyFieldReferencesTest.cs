using Lighthouse.Backend.Services.Implementation.Dependencies;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Dependencies
{
    /// <summary>
    /// A column a person types into, read as a list. Everything here is about being forgiving: nobody
    /// writing "1234; 5678" in a spreadsheet-shaped field is going to be told they got the format wrong,
    /// so the reading has to accept what they will actually write.
    /// </summary>
    [TestFixture]
    [Category("epic-4365-dependencies")]
    public class DependencyFieldReferencesTest
    {
        private static readonly string[] BothOfThem = ["1234", "5678"];

        private static readonly string[] TheOneItNames = ["PROJ-17"];

        private static readonly string[] ThreeEntriesOneOfWhichIsNonsense = ["1234", "see the wiki", "5678"];

        private static readonly string[] TheSameOneTwice = ["1234", "1234"];

        [TestCase("1234,5678")]
        [TestCase("1234;5678")]
        [TestCase(" 1234 ; 5678 ")]
        [TestCase("1234;5678;")]
        [TestCase("1234,5678,,")]
        [TestCase("1234;,5678")]
        [TestCase("\t1234\t,\n5678\n")]
        public void In_AFieldNamingTwo_YieldsBothInTheOrderTheyWereWritten(string whatSomebodyTyped)
        {
            Assert.That(DependencyFieldReferences.In(whatSomebodyTyped), Is.EqualTo(BothOfThem));
        }

        [TestCase("PROJ-17")]
        [TestCase(" PROJ-17 ")]
        [TestCase("PROJ-17;")]
        public void In_AFieldNamingOne_YieldsThatOne(string whatSomebodyTyped)
        {
            Assert.That(DependencyFieldReferences.In(whatSomebodyTyped), Is.EqualTo(TheOneItNames));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(",")]
        [TestCase(" ; , ")]
        public void In_AFieldNamingNothing_YieldsNothingAndSaysNothingIsWrong(string? whatSomebodyTyped)
        {
            Assert.That(DependencyFieldReferences.In(whatSomebodyTyped), Is.Empty);
        }

        /// <summary>
        /// The field is maintained by hand, so it will contain typos. A reading that threw the list away on
        /// the first entry it did not like would be worse than no list: three good references would vanish
        /// because of a fourth, and nothing on the screen would say why.
        /// </summary>
        [Test]
        public void In_AFieldWithSomethingUnrecognisableInTheMiddle_KeepsTheEntriesBesideIt()
        {
            var references = DependencyFieldReferences.In("1234;see the wiki;5678");

            Assert.That(references, Is.EqualTo(ThreeEntriesOneOfWhichIsNonsense),
                "Whether an entry names anything is settled later, against the Features actually held here - "
                + "this reading only says where one entry ends and the next begins.");
        }

        [Test]
        public void In_AFieldNamingTheSameOneTwice_SaysSoRatherThanDecidingItForTheReader()
        {
            Assert.That(DependencyFieldReferences.In("1234,1234"), Is.EqualTo(TheSameOneTwice),
                "Duplicates are collapsed once the references are keyed to the Feature that waits, in the one "
                + "place that already has to do it for the tracker's own links.");
        }
    }
}
