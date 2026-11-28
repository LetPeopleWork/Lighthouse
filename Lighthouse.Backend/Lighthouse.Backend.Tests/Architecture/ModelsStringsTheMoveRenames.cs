namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Every string of the test project that names a free Models type, or its file, by its Models name, with the text it
    /// reads once the type has moved to its owner. The list only shrinks: an entry leaves when its type has moved and the
    /// string was renamed.
    /// </summary>
    internal static class ModelsStringsTheMoveRenames
    {
        public static IReadOnlyList<RenamedString> Entries { get; } =
        [
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessApplicationLayoutArchUnitTest.cs", "Lighthouse.Backend.Models.Authorization.RbacGuardRequirement", "Lighthouse.Backend.Modules.Access.Domain.RbacGuardRequirement"),
            new("Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AccessApplicationLayoutArchUnitTest.cs", "Lighthouse.Backend.Models.Authorization.RbacOperationResult", "Lighthouse.Backend.Modules.Access.Domain.RbacOperationResult"),
        ];
    }
}
