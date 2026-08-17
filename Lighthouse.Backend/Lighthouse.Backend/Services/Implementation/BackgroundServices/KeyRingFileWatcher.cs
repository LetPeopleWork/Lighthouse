using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Microsoft.Extensions.Hosting;

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices
{
    // An operator who adds a key to the secret their own store owns should not have to restart anything for
    // it to take effect, and this is the whole reason the ring arrives as a mounted file rather than as an
    // environment variable, which cannot change under a running process.
    //
    // The file is re-read on a timer rather than subscribed to. A cluster replaces a projected secret by
    // writing a new directory and moving a link, so a subscription registered on the file is still watching
    // something nothing will ever write to again, and it never fires. A re-read cannot be defeated that way.
    //
    // Nothing that fails to read replaces what is running. The ring in force is known to work - every
    // credential on the instance was written under it - and content arriving in a file is known to work only
    // once it has been read, so a file that will not read leaves the instance exactly where it was.
    //
    // This sits apart from the rest of the key handling for one reason: everything in that namespace is
    // barred from writing to a log at all, so that no line can carry key material by accident, and it can be
    // barred because every one of those types has a caller to hand its sentence to. A timer has no caller. It
    // has to say what it did itself, which is why it lives out here beside the other type that must -
    // CryptoService - and why what it writes is pinned by a test of its own.
    public sealed class KeyRingFileWatcher : BackgroundService
    {
        public const string IntervalSettingKey = "Encryption:KeysReloadSeconds";

        private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);

        private readonly MountedFileKeyRingSource mountedFile;

        private readonly IEncryptionKeyRingHolder holder;

        private readonly TimeProvider timeProvider;

        private readonly TimeSpan interval;

        private readonly ILogger<KeyRingFileWatcher> logger;

        private string? contentAlreadyJudged;

        private string? failureAlreadyReported;

        public KeyRingFileWatcher(
            MountedFileKeyRingSource mountedFile,
            IEncryptionKeyRingHolder holder,
            TimeProvider timeProvider,
            TimeSpan interval,
            ILogger<KeyRingFileWatcher> logger)
        {
            ArgumentNullException.ThrowIfNull(mountedFile);
            ArgumentNullException.ThrowIfNull(holder);
            ArgumentNullException.ThrowIfNull(timeProvider);
            ArgumentNullException.ThrowIfNull(logger);

            this.mountedFile = mountedFile;
            this.holder = holder;
            this.timeProvider = timeProvider;
            this.interval = interval;
            this.logger = logger;
        }

        // Thirty seconds is well inside an operator's own round trip of editing a secret and going to look at
        // the panel, and an interval that is no interval at all is treated as one that was not set: refusing
        // to start over it would take an instance down for a value that changes nothing but how often a file
        // is read.
        public static TimeSpan IntervalFrom(int? configuredSeconds)
        {
            return configuredSeconds is > 0 ? TimeSpan.FromSeconds(configuredSeconds.Value) : DefaultInterval;
        }

        public void ReadOnce()
        {
            string? contents;

            try
            {
                contents = mountedFile.ReadContents();
            }
            catch (InvalidOperationException unreadable)
            {
                ReportUnreadable(unreadable.Message);
                return;
            }
            catch (IOException unreadable)
            {
                ReportUnreadable(unreadable.Message);
                return;
            }
            catch (UnauthorizedAccessException unreadable)
            {
                ReportUnreadable(unreadable.Message);
                return;
            }

            failureAlreadyReported = null;

            // Content this instance has already made up its mind about is not judged twice. Without that, a
            // file an operator got wrong would be complained about every half minute for as long as it sat
            // there, and the one message worth reading would be buried under copies of itself.
            if (contents is null || string.Equals(contents, contentAlreadyJudged, StringComparison.Ordinal))
            {
                return;
            }

            contentAlreadyJudged = contents;

            Apply(contents);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(interval, timeProvider);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    ReadOnce();
                }
            }
            catch (OperationCanceledException)
            {
                // Shutting down is not a failure to report.
            }
        }

        private void Apply(string contents)
        {
            EncryptionKeyRing candidate;

            try
            {
                // The published key goes on the end of a reloaded ring for the same reason it goes on the end
                // of a resolved one: an instance that upgraded still holds secrets written under it, and a
                // rotation is no moment to make them unreadable.
                candidate = mountedFile.RingFrom(contents).WithLegacyDefault();
            }
            catch (InvalidOperationException defect)
            {
                logger.LogError(defect, "encryption.keyring.rejected {Defect}", defect.Message);
                return;
            }

            var inForce = holder.Current;

            if (candidate.Equals(inForce))
            {
                return;
            }

            holder.Replace(candidate);

            Announce(candidate, inForce);
        }

        // A key that went away is applied rather than argued with, because custody belongs to whoever owns
        // the secret. It is said out loud because the secrets still written under that key stop being
        // readable at this moment, and nothing else that happens afterwards will point at the reason.
        private void Announce(EncryptionKeyRing now, EncryptionKeyRing before)
        {
            var noLongerHeld = IdsOn(before).Except(IdsOn(now), StringComparer.Ordinal).ToList();

            if (noLongerHeld.Count > 0)
            {
                logger.LogWarning(
                    "encryption.keyring.reloaded {KeyIds} {KeysNoLongerHeld}",
                    string.Join(", ", IdsOn(now)),
                    string.Join(", ", noLongerHeld));

                return;
            }

            logger.LogInformation("encryption.keyring.reloaded {KeyIds}", string.Join(", ", IdsOn(now)));
        }

        private void ReportUnreadable(string reason)
        {
            if (string.Equals(reason, failureAlreadyReported, StringComparison.Ordinal))
            {
                return;
            }

            failureAlreadyReported = reason;

            logger.LogError("encryption.keyring.unreadable {Reason}", reason);
        }

        private static List<string> IdsOn(EncryptionKeyRing ring)
        {
            return [.. ring.RetiredKeys.Prepend(ring.ActiveKey).Select(key => key.Id)];
        }
    }
}
