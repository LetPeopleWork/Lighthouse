namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// A value a save may leave out. Left out, the stored value stays; sent, even as null, it replaces the
    /// stored one. A plain nullable cannot tell the two apart, because both arrive as null.
    /// </summary>
    internal readonly record struct SaveField<T>(T Value, bool Sent)
    {
        public T Or(T stored) => Sent ? Value : stored;

        public TStored Or<TStored>(TStored stored, Func<T, TStored> toStored) => Sent ? toStored(Value) : stored;
    }
}
