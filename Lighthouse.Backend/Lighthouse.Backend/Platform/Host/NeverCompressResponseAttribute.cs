namespace Lighthouse.Backend.Platform.Host
{
    /// <summary>
    /// Put on a controller or hub whose answers can hold a secret or log text next to text an attacker can reach.
    /// Every endpoint of the class then answers uncompressed, whatever its routes are called, because the
    /// compression rule reads this marker from the answering endpoint rather than from a list of paths.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NeverCompressResponseAttribute : Attribute
    {
    }
}
