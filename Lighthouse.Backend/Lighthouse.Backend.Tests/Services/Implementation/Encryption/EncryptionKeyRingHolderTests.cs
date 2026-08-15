using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class EncryptionKeyRingHolderTests
    {
        private const int ReadCount = 1_000;

        private const int ReplaceCount = 100;

        private const int GenerationDigits = 3;

        [Test]
        public void Current_ReturnsTheRingTheHolderWasConstructedWith()
        {
            var ring = RingForGeneration(0);

            var holder = new EncryptionKeyRingHolder(ring);

            Assert.That(holder.Current, Is.SameAs(ring));
        }

        [Test]
        public void Replace_SwapsTheRing_AndSubsequentReadsSeeTheNewOne()
        {
            var holder = new EncryptionKeyRingHolder(RingForGeneration(0));
            var replacement = RingForGeneration(1);

            holder.Replace(replacement);

            Assert.That(holder.Current, Is.SameAs(replacement));
        }

        // Each generation's ring is derivable from its own active key id, so any observed ring can be
        // rebuilt and compared. A read that saw a half-applied swap would not match any generation.
        [Test]
        public void Current_ReadWhileTheRingIsReplaced_NeverObservesATornOrNullRing()
        {
            var replacements = RingsForGenerations(ReplaceCount);
            var holder = new EncryptionKeyRingHolder(replacements[0]);
            var inconsistentReads = new ConcurrentBag<string>();

            Parallel.Invoke(InterleavedReadsAndReplaces(holder, replacements, inconsistentReads));

            Assert.That(inconsistentReads, Is.Empty);
        }

        // A holder with no ring cannot answer the one question it exists for, and every secret read after that
        // point would fail somewhere far from the mistake. It says so at the moment it is built instead.
        [Test]
        public void Holder_BuiltWithoutARing_IsRefused()
        {
            Assert.That(() => new EncryptionKeyRingHolder(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Replace_WithNoRing_IsRefusedAndLeavesTheCurrentRingInPlace()
        {
            var ring = RingForGeneration(0);
            var holder = new EncryptionKeyRingHolder(ring);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => holder.Replace(null!), Throws.ArgumentNullException);
                Assert.That(holder.Current, Is.EqualTo(ring));
            }
        }

        private static Action[] InterleavedReadsAndReplaces(EncryptionKeyRingHolder holder, EncryptionKeyRing[] replacements, ConcurrentBag<string> inconsistentReads)
        {
            var actions = new List<Action>(ReadCount + ReplaceCount);
            var readsPerReplace = ReadCount / ReplaceCount;

            foreach (var replacement in replacements)
            {
                actions.Add(() => holder.Replace(replacement));

                for (var read = 0; read < readsPerReplace; read++)
                {
                    actions.Add(() => RecordIfInconsistent(holder.Current, inconsistentReads));
                }
            }

            return [.. actions];
        }

        private static void RecordIfInconsistent(EncryptionKeyRing? observed, ConcurrentBag<string> inconsistentReads)
        {
            if (observed is null)
            {
                inconsistentReads.Add("no ring at all");
                return;
            }

            var activeKeyId = observed.ActiveKey.Id;
            var generation = int.Parse(activeKeyId[^GenerationDigits..], CultureInfo.InvariantCulture);

            if (!observed.Equals(RingForGeneration(generation)))
            {
                inconsistentReads.Add(activeKeyId);
            }
        }

        private static EncryptionKeyRing[] RingsForGenerations(int count)
        {
            return [.. Enumerable.Range(0, count).Select(RingForGeneration)];
        }

        private static EncryptionKeyRing RingForGeneration(int generation)
        {
            return new EncryptionKeyRing(
                KeyWith($"k-active-{generation.ToString("D3", CultureInfo.InvariantCulture)}"),
                KeyWith($"k-retired-{generation.ToString("D3", CultureInfo.InvariantCulture)}"));
        }

        private static EncryptionKey KeyWith(string keyId)
        {
            return new EncryptionKey(keyId, SHA256.HashData(Encoding.UTF8.GetBytes(keyId)));
        }
    }
}
