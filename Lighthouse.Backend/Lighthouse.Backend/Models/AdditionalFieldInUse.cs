namespace Lighthouse.Backend.Models
{
    public sealed record RemovedAdditionalField(int Id, string DisplayName);

    /// <param name="Name">How the admin knows the default, for example "Teams' Parent Override Field".</param>
    public sealed record DefaultNamingAField(string Name, int? FieldId);

    /// <param name="OwnFieldIds">The fields the Team or Portfolio chose itself, one per setting; a setting that follows a default is not among them.</param>
    public sealed record OwnerNamingFields(string Name, IReadOnlyCollection<int?> OwnFieldIds);

    /// <summary>
    /// A field a save removes while a default or a Team or Portfolio still reads it. Removing it would leave
    /// them reading a field that is not there, so the whole save is refused and says who uses it.
    /// </summary>
    public sealed record AdditionalFieldInUse(string FieldName, IReadOnlyList<string> Defaults, IReadOnlyList<string> Owners)
    {
        public static List<AdditionalFieldInUse> AmongTheRemoved(
            IEnumerable<RemovedAdditionalField> removed,
            IEnumerable<DefaultNamingAField> defaults,
            IEnumerable<OwnerNamingFields> owners)
        {
            var allDefaults = defaults.ToList();
            var allOwners = owners.ToList();

            return [.. removed
                .Select(field => new AdditionalFieldInUse(
                    field.DisplayName,
                    [.. allDefaults.Where(theDefault => theDefault.FieldId == field.Id).Select(theDefault => theDefault.Name)],
                    [.. allOwners.Where(owner => owner.OwnFieldIds.Contains(field.Id)).Select(owner => owner.Name)]))
                .Where(field => field.Defaults.Count > 0 || field.Owners.Count > 0)];
        }

        public static string Refusal(IEnumerable<AdditionalFieldInUse> fieldsInUse)
            => string.Join(" ", fieldsInUse.Select(field => field.Sentence())) + " Nothing was saved.";

        private string Sentence()
        {
            var namedByDefaults = $"It is {Listed(Defaults.Select(theDefault => $"the {theDefault} default"))}";
            var ownedBy = Owners.Count == 1
                ? $"{Owners[0]} uses it as its own"
                : $"{Listed(Owners)} use it as their own";

            var usedBy = (Defaults.Count > 0, Owners.Count > 0) switch
            {
                (true, true) => $"{namedByDefaults}, and {ownedBy}",
                (true, false) => namedByDefaults,
                _ => ownedBy,
            };

            return $"\"{FieldName}\" cannot be removed. {usedBy}.";
        }

        private static string Listed(IEnumerable<string> names)
        {
            var all = names.ToList();
            return all.Count <= 1
                ? string.Concat(all)
                : $"{string.Join(", ", all[..^1])} and {all[^1]}";
        }
    }
}
