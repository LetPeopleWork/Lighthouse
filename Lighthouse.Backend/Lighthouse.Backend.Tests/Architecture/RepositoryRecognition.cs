using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// How the architecture rules tell a repository, a repository contract and the stored records apart from the
    /// rest of the code, kept in one place. A rule that recognised them by a namespace of its own would quietly
    /// stop seeing a repository the day it moves into a module, and still pass; with one definition, a move
    /// that the definition does not know about shows here.
    /// </summary>
    internal static partial class RepositoryRecognition
    {
        /// <summary>Where the concrete repositories' source files live, relative to the production project.</summary>
        public const string RepositoriesFolder = "Services/Implementation/Repositories/";

        public const string ConcreteRepositoriesNamespace = "Lighthouse.Backend.Services.Implementation.Repositories";

        public const string RepositoryContractsNamespace = "Lighthouse.Backend.Services.Interfaces.Repositories";

        /// <summary>A type whose full name matches this is a repository contract.</summary>
        public const string RepositoryContracts = @"^Lighthouse\.Backend\.Services\.Interfaces\.Repositories\.[^.]+$";

        /// <summary>The namespaces that hold the stored records: the database context and the repositories.</summary>
        public const string StoredRecordsPattern =
            @"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.Repositories)($|\..*)";

        /// <summary>The namespaces of the persistence platform: the stored records and what keeps them.</summary>
        public const string PlatformPersistencePattern =
            @"^Lighthouse\.Backend\.(Data|Services\.(Implementation|Interfaces)\.(Repositories|DatabaseManagement|DomainEvents|Seeding|OAuth))($|\..*)";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        /// <summary>Whether a production source file, by its path relative to the production project, is a repository's own file.</summary>
        public static bool IsInRepositoriesFolder(string relativePath)
            => relativePath.StartsWith(RepositoriesFolder, StringComparison.Ordinal);

        /// <summary>Whether the type is a repository: a concrete one counts by where it lives, a contract by its name.</summary>
        public static bool IsRepository(TypeReference type) => IsRepository(IlDependencies.NamespaceOf(type), type.Name);

        // The repository-contracts namespace also holds service contracts that are not repositories, so a contract
        // counts by its name, and a concrete repository counts by where it lives.
        public static bool IsRepository(string namespaceName, string typeName)
            => namespaceName == ConcreteRepositoriesNamespace || RepositoryContractName().IsMatch(WithoutGenericArity(typeName));

        public static bool IsRepositoryContract(string typeFullName)
            => Regex.IsMatch(typeFullName, RepositoryContracts, RegexOptions.None, PatternTimeout);

        public static bool HoldsStoredRecords(string namespaceName)
            => Regex.IsMatch(namespaceName, StoredRecordsPattern, RegexOptions.None, PatternTimeout);

        public static bool IsPlatformPersistence(string namespaceName)
            => Regex.IsMatch(namespaceName, PlatformPersistencePattern, RegexOptions.None, PatternTimeout);

        private static string WithoutGenericArity(string typeName)
        {
            var arity = typeName.IndexOf('`');

            return arity < 0 ? typeName : typeName[..arity];
        }

        [GeneratedRegex(@"^I\w*Repository$")]
        private static partial Regex RepositoryContractName();
    }
}
