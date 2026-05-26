using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class TimeInStateSeamArchUnitTest
    {
        private static readonly HashSet<Type> TransitionRepositoryConsumers =
        [
            typeof(WorkItemService),
            typeof(WorkItemStateTransitionRepository),
            typeof(TeamMetricsService)
        ];

        [Test]
        public void WorkItem_HasNoMappedNavigationProperty_ToWorkItemStateTransition()
        {
            var mappedTransitionCollections = new List<string>();

            foreach (var member in WorkItemPersistedMembers())
            {
                if (!IsTransitionCollection(MemberType(member)))
                {
                    continue;
                }

                if (member.GetCustomAttribute<NotMappedAttribute>() == null)
                {
                    mappedTransitionCollections.Add($"{member.DeclaringType?.Name}.{member.Name}");
                }
            }

            Assert.That(mappedTransitionCollections, Is.Empty,
                "ADR-015 architectural enforcement: WorkItem must not own a mapped collection " +
                "navigation property to WorkItemStateTransition. The foreign key lives on the child " +
                "(WorkItemStateTransition.WorkItemId), not as an owned collection on the parent. " +
                "The following members are mapped collections of WorkItemStateTransition: " +
                string.Join(", ", mappedTransitionCollections) + ". " +
                "Remove the navigation property (a [NotMapped] sync-input vehicle is allowed), or " +
                "update the architectural decision record first and amend this test.");
        }

        [Test]
        public void WorkItemService_IsSoleWriter_OfTransitionsAndCurrentStateEnteredAt()
        {
            var productionAssembly = typeof(WorkItemService).Assembly;
            var unauthorisedConsumers = new List<string>();

            foreach (var type in productionAssembly.GetTypes())
            {
                if (TransitionRepositoryConsumers.Contains(type))
                {
                    continue;
                }

                if (DependsOnTransitionRepository(type))
                {
                    unauthorisedConsumers.Add(type.FullName ?? type.Name);
                }
            }

            Assert.That(unauthorisedConsumers, Is.Empty,
                "DDD-6 / ADR-016 / ADR-017 architectural enforcement: WorkItemService is the sole writer " +
                "of WorkItemStateTransition rows and the sole mutator of WorkItem.CurrentStateEnteredAt. " +
                "Only the sanctioned consumers may take a constructor or field dependency on " +
                "IWorkItemStateTransitionRepository: WorkItemService (the writer), the repository adapter " +
                "that implements it, and the metrics-service readers sanctioned per ADR-019/DDD-9 " +
                "(TeamMetricsService reads transitions to compute per-state age-in-state percentiles). " +
                "The following types depend on the transition repository without sanction: " +
                string.Join(", ", unauthorisedConsumers) + ". " +
                "Route transition writes through WorkItemService.UpdateWorkItemsForTeam instead. If a new " +
                "writer or reader is genuinely required, update the architectural decision record first and amend this test.");
        }

        private static IEnumerable<MemberInfo> WorkItemPersistedMembers()
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            foreach (var property in typeof(WorkItem).GetProperties(bindingFlags))
            {
                yield return property;
            }

            foreach (var field in typeof(WorkItem).GetFields(bindingFlags))
            {
                yield return field;
            }
        }

        private static Type MemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        private static bool IsTransitionCollection(Type type)
        {
            if (type == typeof(string) || type == typeof(WorkItemStateTransition))
            {
                return false;
            }

            var elementTypes = CollectionElementTypes(type);
            return elementTypes.Any(elementType => elementType == typeof(WorkItemStateTransition));
        }

        private static IEnumerable<Type> CollectionElementTypes(Type type)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (elementType != null)
                {
                    yield return elementType;
                }

                yield break;
            }

            if (!typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
            {
                yield break;
            }

            foreach (var argument in type.GetGenericArguments())
            {
                yield return argument;
            }
        }

        private static bool DependsOnTransitionRepository(Type type)
        {
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            foreach (var constructor in type.GetConstructors(bindingFlags))
            {
                if (constructor.GetParameters().Any(parameter => IsTransitionRepository(parameter.ParameterType)))
                {
                    return true;
                }
            }

            foreach (var field in type.GetFields(bindingFlags))
            {
                if (IsTransitionRepository(field.FieldType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTransitionRepository(Type type)
        {
            return typeof(IWorkItemStateTransitionRepository).IsAssignableFrom(type);
        }
    }
}
