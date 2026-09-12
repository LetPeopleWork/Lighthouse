namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// What a browser answered when it was asked whether Lighthouse may send usage data.
    /// </summary>
    public enum UsageDataDecision
    {
        // These numbers are what is written to the database, so they may never be reordered and a
        // retired one may never be reused - an old row would silently come back as a different answer.
        Granted = 0,

        Declined = 1,

        // Someone who granted and then changed their mind is not the same as someone who said no from
        // the start: a refusal is respected by not asking again, while a withdrawal has to leave the
        // door open, or changing your mind once would lock you out of ever being asked again.
        Revoked = 2,
    }
}
