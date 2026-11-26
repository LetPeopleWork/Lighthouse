using System.Reflection;
using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>What a rule pattern is matched against by the rule that uses it.</summary>
    internal enum PatternSubject
    {
        /// <summary>The namespace of the outermost type, as the IL and ArchUnit namespace selections read it.</summary>
        Namespace,

        /// <summary>The full name of the type.</summary>
        FullName,
    }

    /// <summary>What a rule pattern stands for, which decides whether it must follow a moving type when the type moves.</summary>
    internal enum PatternRole
    {
        /// <summary>A part of the code (a context, the platform, the update code): it must keep seeing the type after the move.</summary>
        Context,

        /// <summary>An old location (the Services folder): it must not see the type in its new place.</summary>
        Place,

        /// <summary>
        /// The front door of one context, the layers other code may reach it through: like a part of the code, it must see the
        /// type under its old and its new name when the type moves into that context's published namespace, and take in no
        /// other context.
        /// </summary>
        FrontDoor,
    }

    /// <summary>
    /// How one rule pattern is read: on which subject, in which role, and why. A pattern built from another one by
    /// concatenation names that component and is read the way the component is read.
    /// </summary>
    internal sealed record PatternDeclaration(PatternSubject? Subject, PatternRole? Role, string? CompositeOf, string Reason)
    {
        public static PatternDeclaration Context(PatternSubject subject, string reason) => new(subject, PatternRole.Context, null, reason);

        public static PatternDeclaration Place(PatternSubject subject, string reason) => new(subject, PatternRole.Place, null, reason);

        public static PatternDeclaration FrontDoor(PatternSubject subject, string reason) => new(subject, PatternRole.FrontDoor, null, reason);

        public static PatternDeclaration Composite(string component, string reason) => new(null, null, component, reason);
    }

    /// <summary>A type that moves: found today in its old namespace, after the move in its new one, inside the area it moves with.</summary>
    internal sealed record MovingType(string Name, string Area, string OldNamespace, string NewNamespace)
    {
        public string OldFullName => $"{OldNamespace}.{Name}";

        public string NewFullName => $"{NewNamespace}.{Name}";
    }

    /// <summary>A string in a file that names a moving type or its file, and the text it reads after the move.</summary>
    internal sealed record RenamedString(string File, string OldText, string NewText);

    /// <summary>
    /// One namespace move: the types that move, the strings it renames, the pattern of the namespace the types move into,
    /// the token that marks a rule alternative naming that new place, and the areas of Services that move as a whole.
    /// </summary>
    internal sealed record NamespaceMove(
        IReadOnlyList<MovingType> MovingTypes,
        IReadOnlyList<RenamedString> StringsTheMoveRenames,
        string NewPlace,
        Regex NewPlaceToken,
        IReadOnlyList<string> MovingAreasOfServices);

    /// <summary>
    /// The declared subject and role of every rule pattern that sees a moving type, and the patterns that saw a moving type
    /// only through the folder it lived in, keyed by pattern (class and field) and the name of the moving type.
    /// </summary>
    internal sealed record PatternRoleMap(
        IReadOnlyDictionary<string, PatternDeclaration> Roles,
        IReadOnlyDictionary<(string Field, string Type), string> FolderOnlyMatches);

    /// <summary>How a rule pattern breaks its role when the types move.</summary>
    internal enum RoleBreak
    {
        NotDeclared,
        StopsSeeingTheType,
        SeesTheTypeInItsNewPlace,
        SeesAnAreaItNeverSaw,
        KeepsAPlatformAlternativeAsAnOldLocation,
        FollowsATypeItSawOnlyByFolder,
    }

    /// <summary>A constant or static read-only string field that a rule matches names against, keyed by class and field.</summary>
    internal sealed record RulePattern(string Field, string Value);

    /// <summary>The production namespaces and full names outside the place the types move into.</summary>
    internal sealed record OutsideNames(IReadOnlyList<string> Namespaces, IReadOnlyList<string> FullNames);

    /// <summary>One rule pattern that would not keep its meaning for one moving type, with its answers under the old and the new subject.</summary>
    internal sealed record PatternBreak(RulePattern Pattern, string Declared, MovingType Type, string OldSubject, bool UnderTheOld, string NewSubject, bool UnderTheNew, RoleBreak Break)
    {
        public string Describe()
            => Break == RoleBreak.NotDeclared
                ? $"    not declared, sees {Type.Name} ({Type.OldFullName} -> {Type.NewFullName})"
                : $"    {Break}: {Type.Name}: {OldSubject} -> {Answer(UnderTheOld)}, {NewSubject} -> {Answer(UnderTheNew)}";

        private static string Answer(bool matches) => matches ? "matches" : "does not match";
    }

    /// <summary>
    /// The law a namespace move must keep: every architecture rule that finds a moving type by name keeps its meaning after
    /// the move. A part of the code keeps seeing the type in its new place, and only by taking in the whole area the type
    /// moves into; an old location does not see it there; an alternative added for the move reaches nothing outside the new
    /// place; and no string in the rule files names a moving area or type by its old path unless the move renames it.
    /// The law is asked with one move and the declared roles of the rule patterns that see its types.
    /// </summary>
    internal sealed class NamespaceMoveLaw
    {
        public static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        private static readonly Regex StringLiteral = new(
            @"'(?:[^'\\\r\n]|\\.)'|(?:\$@|@\$|@)""(?<verbatim>(?:[^""]|"""")*)""|\$?""(?<regular>(?:[^""\\\r\n]|\\.)*)""",
            RegexOptions.None,
            PatternTimeout);

        private static readonly Regex NamesTheBackend = new(@"Lighthouse\\?\.Backend", RegexOptions.None, PatternTimeout);

        private static readonly string RuleNamespace = typeof(NamespaceMoveLaw).Namespace!;

        private readonly NamespaceMove move;

        private readonly PatternRoleMap roles;

        private readonly Lazy<List<Regex>> oldPathReferences;

        public NamespaceMoveLaw(NamespaceMove move, PatternRoleMap roles)
        {
            this.move = move;
            this.roles = roles;
            oldPathReferences = new(() => [.. OldPathReferencesOf(move)]);
        }

        public List<RulePattern> RulePatternsOf(IEnumerable<Type> types)
        {
            var renamed = move.StringsTheMoveRenames;

            return [.. from type in types
                where !type.ContainsGenericParameters
                from field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                where field.FieldType == typeof(string) && (field.IsLiteral || field.IsInitOnly)
                let value = (field.IsLiteral ? field.GetRawConstantValue() : field.GetValue(null)) as string
                where value is not null && IsARulePattern(type, value)
                where !renamed.Any(entry => (entry.OldText == value || entry.NewText == value) && FileStem(entry.File) == OutermostType(type).Name)
                where !(field.IsInitOnly && IsTheFullNameOfAMovingType(value))
                select new RulePattern(FieldName(type, field), value)];
        }

        public HashSet<string> PatternValuesItGoverns(IEnumerable<RulePattern> patterns)
            => [.. patterns
                .Where(pattern => move.MovingTypes.Any(type => SubjectsOf(type).Any(subject => IsMatch(subject, pattern.Value))))
                .Select(pattern => pattern.Value)];

        public List<PatternBreak> RoleBreaksOf(IEnumerable<RulePattern> patterns)
        {
            var breaks = new List<PatternBreak>();

            foreach (var pattern in patterns)
            {
                var declared = Resolve(pattern.Field);
                if (declared is null)
                {
                    breaks.AddRange(
                        from type in move.MovingTypes
                        where IsMatch(type.OldNamespace, pattern.Value) || IsMatch(type.NewNamespace, pattern.Value) || IsMatch(type.OldFullName, pattern.Value) || IsMatch(type.NewFullName, pattern.Value)
                        select new PatternBreak(pattern, "not declared", type, string.Empty, false, string.Empty, false, RoleBreak.NotDeclared));
                    continue;
                }

                var (subject, role) = declared.Value;
                var answers = move.MovingTypes
                    .Select(type => (Type: type, Old: OldSubject(type, subject), New: NewSubject(type, subject)))
                    .Select(asked => (asked.Type, asked.Old, asked.New, UnderTheOld: IsMatch(asked.Old, pattern.Value), UnderTheNew: IsMatch(asked.New, pattern.Value)))
                    .ToList();
                var seenOnlyByFolder = PatternAndItsComponents(pattern.Field)
                    .SelectMany(field => roles.FolderOnlyMatches.Keys.Where(match => match.Field == field).Select(match => match.Type))
                    .ToHashSet(StringComparer.Ordinal);
                var areasSeenBefore = answers.Where(answer => answer.UnderTheOld && !seenOnlyByFolder.Contains(answer.Type.Name)).Select(answer => answer.Type.Area).ToHashSet(StringComparer.Ordinal);

                if (role == PatternRole.Place && move.NewPlaceToken.IsMatch(pattern.Value))
                {
                    var type = answers.Where(answer => answer.UnderTheOld).Select(answer => answer.Type).FirstOrDefault() ?? move.MovingTypes[0];
                    breaks.Add(new PatternBreak(
                        pattern,
                        $"{subject}, {role}",
                        type,
                        OldSubject(type, subject),
                        IsMatch(OldSubject(type, subject), pattern.Value),
                        NewSubject(type, subject),
                        IsMatch(NewSubject(type, subject), pattern.Value),
                        RoleBreak.KeepsAPlatformAlternativeAsAnOldLocation));
                }

                foreach (var answer in answers)
                {
                    var readAs = role == PatternRole.FrontDoor ? PatternRole.Context : role;
                    var roleBreak = (seenOnlyByFolder.Contains(answer.Type.Name) ? PatternRole.Place : readAs, answer.UnderTheOld, answer.UnderTheNew) switch
                    {
                        (PatternRole.Place, _, true) when seenOnlyByFolder.Contains(answer.Type.Name) => RoleBreak.FollowsATypeItSawOnlyByFolder,
                        (PatternRole.Context, true, false) => RoleBreak.StopsSeeingTheType,
                        (PatternRole.Context, false, true) when !areasSeenBefore.Contains(answer.Type.Area) => RoleBreak.SeesAnAreaItNeverSaw,
                        (PatternRole.Place, _, true) => RoleBreak.SeesTheTypeInItsNewPlace,
                        _ => (RoleBreak?)null,
                    };

                    if (roleBreak is not null)
                    {
                        breaks.Add(new PatternBreak(pattern, $"{subject}, {role}", answer.Type, answer.Old, answer.UnderTheOld, answer.New, answer.UnderTheNew, roleBreak.Value));
                    }
                }
            }

            return breaks;
        }

        public OutsideNames NamesOutsideTheNewPlace(ModuleDefinition production)
        {
            var typesOutside = IlDependencies.TypesIn(production, @"^Lighthouse\.Backend(\.|$)")
                .Where(typeName => !IsMatch(typeName, move.NewPlace))
                .ToList();
            var namespacesOutside = production.Types
                .Select(type => type.Namespace)
                .Where(name => IsMatch(name, @"^Lighthouse\.Backend(\.|$)") && !IsMatch(name, move.NewPlace))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return new OutsideNames(namespacesOutside, typesOutside);
        }

        public List<string> AlternativesReachingOutside(IEnumerable<RulePattern> patterns, OutsideNames outside)
        {
            var namespacesOutside = outside.Namespaces;
            var typesOutside = outside.FullNames;

            return [.. from pattern in patterns
                let withoutTheNewPlace = move.NewPlaceToken.Replace(pattern.Value, "(?!)")
                where withoutTheNewPlace != pattern.Value
                let subject = Resolve(pattern.Field)?.Subject
                from name in subject switch
                {
                    PatternSubject.Namespace => namespacesOutside,
                    PatternSubject.FullName => typesOutside,
                    _ => namespacesOutside.Concat(typesOutside),
                }
                where IsMatch(name, pattern.Value) && !IsMatch(name, withoutTheNewPlace)
                select $"{pattern.Field} = {pattern.Value} matches {name}"];
        }

        public List<string> OldPathStringsIn(string relativePath, string source, HashSet<string> governedPatterns)
        {
            var renamedHere = move.StringsTheMoveRenames
                .Where(entry => string.Equals(entry.File, relativePath, StringComparison.Ordinal))
                .Select(entry => entry.OldText)
                .OrderByDescending(oldText => oldText.Length)
                .ToList();
            var withoutComments = ProductionSource.WithoutComments(source);
            var found = new List<string>();

            foreach (Match literal in StringLiteral.Matches(withoutComments))
            {
                if (!literal.Groups["verbatim"].Success && !literal.Groups["regular"].Success)
                {
                    continue;
                }

                var value = literal.Groups["verbatim"].Success
                    ? literal.Groups["verbatim"].Value.Replace("\"\"", "\"", StringComparison.Ordinal)
                    : literal.Groups["regular"].Value.Replace("\\\\", "\\", StringComparison.Ordinal).Replace("\\\"", "\"", StringComparison.Ordinal);

                if (governedPatterns.Contains(value))
                {
                    continue;
                }

                var remainder = renamedHere.Aggregate(value, (text, oldText) => text.Replace(oldText, " ", StringComparison.Ordinal));
                var normalised = remainder.Replace("\\", string.Empty, StringComparison.Ordinal).Replace('/', '.');

                if (oldPathReferences.Value.Any(reference => reference.IsMatch(normalised)))
                {
                    var line = withoutComments.AsSpan(0, literal.Index).Count('\n') + 1;
                    found.Add($"{relativePath}:{line}: {literal.Value}");
                }
            }

            return found;
        }

        public static bool IsMatch(string input, string pattern) => Regex.IsMatch(input, pattern, RegexOptions.None, PatternTimeout);

        public static string FileStem(string file) => Path.GetFileName(file).Split('.')[0];

        private (PatternSubject Subject, PatternRole Role)? Resolve(string field)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var key = field;
            while (roles.Roles.TryGetValue(key, out var declaration) && seen.Add(key))
            {
                if (declaration.CompositeOf is null)
                {
                    return declaration.Subject is { } subject && declaration.Role is { } role ? (subject, role) : null;
                }

                key = declaration.CompositeOf;
            }

            return null;
        }

        private IEnumerable<string> PatternAndItsComponents(string field)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var key = field; key is not null && seen.Add(key); key = roles.Roles.TryGetValue(key, out var declaration) ? declaration.CompositeOf : null)
            {
                yield return key;
            }
        }

        // A read-only field holding exactly one moving type's full name is read from the type itself (typeof), so it
        // follows the type by compilation; it designates one type and is not a pattern a rule matches namespaces with.
        private bool IsTheFullNameOfAMovingType(string value)
            => move.MovingTypes.Any(type => value == type.OldFullName || value == type.NewFullName);

        private static IEnumerable<Regex> OldPathReferencesOf(NamespaceMove move)
        {
            const string fromTheBackendOrTheStart = @"(?<=^|^\.|[^A-Za-z0-9_.]|[^A-Za-z0-9_.]\.|Lighthouse\.Backend\.)";

            foreach (var area in move.MovingAreasOfServices.Select(Regex.Escape))
            {
                yield return new Regex($@"{fromTheBackendOrTheStart}Services\.(Implementation|Interfaces)\.{area}(?![A-Za-z0-9_]|\.[A-Za-z_])", RegexOptions.None, PatternTimeout);
            }

            foreach (var type in move.MovingTypes)
            {
                var oldNamespace = type.OldNamespace["Lighthouse.Backend.".Length..];
                var name = type.Name.Split('`')[0];
                yield return new Regex($@"{fromTheBackendOrTheStart}{Regex.Escape(oldNamespace)}\.{Regex.Escape(name)}(?![A-Za-z0-9_])", RegexOptions.None, PatternTimeout);
            }
        }

        private static bool IsARulePattern(Type type, string value)
            => NamesTheBackend.IsMatch(value)
                && (value.StartsWith('^') || type.Namespace?.StartsWith(RuleNamespace, StringComparison.Ordinal) == true)
                && ParsesAsAPattern(value);

        private static bool ParsesAsAPattern(string value)
        {
            try
            {
                _ = new Regex(value, RegexOptions.None, PatternTimeout);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string[] SubjectsOf(MovingType type) => [type.OldNamespace, type.NewNamespace, type.OldFullName, type.NewFullName];

        private static string OldSubject(MovingType type, PatternSubject subject) => subject == PatternSubject.Namespace ? type.OldNamespace : type.OldFullName;

        private static string NewSubject(MovingType type, PatternSubject subject) => subject == PatternSubject.Namespace ? type.NewNamespace : type.NewFullName;

        private static Type OutermostType(Type type) => type.DeclaringType is null ? type : OutermostType(type.DeclaringType);

        private static string FieldName(Type type, FieldInfo field)
            => $"{type.FullName![(type.Namespace!.Length + 1)..].Replace('+', '.')}.{field.Name}";
    }
}
