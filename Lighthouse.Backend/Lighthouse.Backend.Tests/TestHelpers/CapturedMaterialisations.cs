using System.Collections.Concurrent;
using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Counts every object the database hands back, by type, for as long as a scenario is listening.
    ///
    /// "This screen no longer reads every forecast of the instance" is a claim about what came out of the
    /// store, so it is read at the store's edge: a histogram row that was loaded is counted here whichever
    /// reader, include or query shape loaded it. A renamed reader, a split query or a projection does not
    /// change what is counted, which is what keeps these assertions from being about how the read is written.
    /// </summary>
    public sealed class CapturedMaterialisations
    {
        private readonly ConcurrentDictionary<Type, int> handedBack = new();

        private volatile bool listening;

        /// <summary>Forgets what was counted so far and counts from here on.</summary>
        public void StartListening()
        {
            handedBack.Clear();
            listening = true;
        }

        public void StopListening() => listening = false;

        /// <summary>How many objects of <typeparamref name="T"/> (or a type derived from it) were read since listening started.</summary>
        public int Of<T>()
            => handedBack.Where(pair => typeof(T).IsAssignableFrom(pair.Key)).Sum(pair => pair.Value);

        public string Describe()
            => string.Join(", ", handedBack.OrderBy(pair => pair.Key.Name, StringComparer.Ordinal).Select(pair => $"{pair.Key.Name} {pair.Value}"));

        /// <summary>Counts what the host's database hands back into <paramref name="captured"/>.</summary>
        public static void CountInto(IServiceCollection services, CapturedMaterialisations captured)
        {
            services.AddSingleton(captured);
            services.ConfigureDbContext<LighthouseAppContext>(options => options.AddInterceptors(Router.Instance));
        }

        private void Count(object entity)
        {
            if (listening)
            {
                handedBack.AddOrUpdate(entity.GetType(), 1, (_, count) => count + 1);
            }
        }

        /// <summary>
        /// Entity Framework builds a new internal service provider for every distinct materialisation interceptor
        /// and refuses after twenty, so every host shares this one and it finds the host's own counter.
        /// </summary>
        private sealed class Router : IMaterializationInterceptor
        {
            public static readonly Router Instance = new();

            public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
            {
                var host = materializationData.Context.GetService<IDbContextOptions>()
                    .FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;

                host?.GetService<CapturedMaterialisations>()?.Count(entity);

                return entity;
            }
        }
    }
}
