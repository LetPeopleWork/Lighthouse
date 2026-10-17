namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Where an owner's setting comes from: the default its connection sets for every owner of its kind,
    /// or a value of the owner's own (where no value at all is a deliberate None). Stored as an int, so
    /// members are only ever appended.
    /// </summary>
    public enum SettingSource
    {
        ConnectionDefault = 0,
        Own = 1,
    }
}
