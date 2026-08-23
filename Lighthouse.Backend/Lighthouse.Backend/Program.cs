using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Health;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Auth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.OAuth;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Encryption;
using Lighthouse.Backend.Startup;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Npgsql;
using System.Data.Common;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Standalone;
using Lighthouse.Backend.API.Swagger;
using Lighthouse.Backend.API.Filters;

namespace Lighthouse.Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args) ?? throw new ArgumentNullException(nameof(args), "WebApplicationBuilder cannot be null");

            // Check if we are running as a Tauri Sidecar
            var isStandalone = Environment.GetEnvironmentVariable("Standalone") == "true";

            EnsureCorsFailsClosed(builder, isStandalone);

            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

                var (keyStore, _) = InitializeKeyStore(builder, isStandalone, StandaloneInitializer.InitializePaths);

                EnsureOAuthStateSecret(builder, keyStore);
                EnsureEncryptionKeyRing(builder, keyStore);

                ConfigureLogging(builder);
                Log.Information("Starting up Lighthouse!");
                Log.Information("Mode: {Mode}", isStandalone ? "Standalone (Tauri)" : "Server (ASP.NET Core)");

                RegisterServices(builder);

                if (!isStandalone)
                {
                    ConfigureHttps(builder);
                }

                ConfigureServices(builder, keyStore);
                ConfigureDatabase(builder);

                var app = builder.Build();

                EnsureOAuthProvidersRegistered(app);

                ConfigureApp(app);

                await RunStartupOrphanedFeatureCleanupAsync(app);

                if (isStandalone)
                {
                    // Register the banner to print once the server is actually up
                    app.Lifetime.ApplicationStarted.Register(() =>
                    {
                        PrintSystemInfo(app, builder);
                    });

                    Log.Information("Backend is ready. Starting web host...");

                    // This is the CRITICAL change: await the blocking run call
                    await app.RunAsync();
                }
                else
                {
                    // Standard dev/production mode logic
                    _ = Task.Run(async () =>
                    {
                        await app.StartAsync();
                        await Task.Delay(500);
                        PrintSystemInfo(app, builder);
                    });

                    await app.WaitForShutdownAsync();
                }
            }
            catch (Exception ex)
            {
                // Vital for sidecar debugging: ensure the error hits StdErr
                await Console.Error.WriteLineAsync($"FATAL: {ex.Message}");
                Log.Fatal(ex, "Application terminated unexpectedly");

                if (builder.Environment.IsEnvironment("Testing"))
                {
                    throw;
                }

                Environment.Exit(1); // Force non-zero exit code on failure
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static void ConfigureApp(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseForwardedHeaders();

            app.UseCors("AllowAll");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lighthouse API V1");
                c.RoutePrefix = "api/docs";
            });

            // Bug #5732: the same options must serve index.html on both the static-file and
            // the SPA fallback path, or deep links hand out a cacheable shell.
            var staticFileOptions = new StaticFileOptions
            {
                // Don't cache index.html to ensure users always get the latest version, but allow caching for other static assets
                OnPrepareResponse = ctx =>
                {
                    // sw.js is the Bug #5732 tombstone: a cached copy would delay the uninstall.
                    if (ctx.File.Name == "index.html" || ctx.File.Name == "sw.js")
                    {
                        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                        ctx.Context.Response.Headers.Pragma = "no-cache";
                        ctx.Context.Response.Headers.Expires = "0";
                    }
                }
            };

            app.UseDefaultFiles();
            app.UseStaticFiles(staticFileOptions);

            app.UseRouting();

            var rateLimitsConfig = app.Services.GetRequiredService<IConfiguration>()
                .GetSection(RateLimitingConfiguration.SectionName)
                .Get<RateLimitingConfiguration>() ?? new RateLimitingConfiguration();

            if (rateLimitsConfig.Enabled)
            {
                app.UseRateLimiter();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.MapHub<UpdateNotificationHub>("api/updateNotificationHub");

            MapHealthEndpoints(app);
            TelemetryConfigurator.MapEndpoints(app);

            app.Lifetime.ApplicationStopping.Register(
                () => app.Services.GetRequiredService<IReadinessState>().BeginDraining());

            app.MapGet("/.well-known/security.txt", async context =>
            {
                var wwwroot = app.Environment.WebRootPath;
                var filePath = Path.Combine(wwwroot, "security.txt");

                if (!File.Exists(filePath))
                {
                    context.Response.StatusCode = 404;
                    return;
                }

                context.Response.ContentType = "text/plain; charset=utf-8";
                await context.Response.SendFileAsync(filePath);
            });

            app.Use(async (context, next) =>
            {
                if (context.GetEndpoint() is null && ShouldNotFallBackToSpaShell(context.Request.Path))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }

                await next();
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
                spa.Options.DefaultPage = "/index.html";
                spa.Options.DefaultPageStaticFileOptions = staticFileOptions;
            });
        }

        // Bug #5732: a request that matched no endpoint must not receive the SPA shell with a 200.
        // A stale client then parses HTML as JSON, and a removed service worker script can never
        // be replaced because its update fetch is answered with a page instead of a script.
        private static readonly string[] NonSpaFileExtensions = [".js", ".mjs", ".css", ".json", ".webmanifest", ".map"];

        private static bool ShouldNotFallBackToSpaShell(PathString path)
        {
            if (path.StartsWithSegments("/api"))
            {
                return true;
            }

            var value = path.Value;

            return value is not null
                   && NonSpaFileExtensions.Any(extension => value.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
        }

        private const string RedisIdentifier = "Redis";


        private static void ConfigureServices(WebApplicationBuilder builder, KeyStoreLocation keyStore)
        {
            var authConfig = LoadAuthenticationConfiguration(builder);

            ConfigureCors(builder, authConfig);
            ForwardedHeadersConfigurator.Configure(builder.Services, builder.Configuration, authConfig);
            ConfigureDataProtection(builder, keyStore);
            ConfigureAuthentication(builder, authConfig);
            ConfigureRateLimiting(builder);
            ConfigureHealthChecks(builder);
            ConfigureGracefulShutdown(builder);
            TelemetryConfigurator.Configure(builder.Services, builder.Configuration);

            builder.Services
                .AddControllers(options =>
                {
                    options.Filters.Add<ConcurrencyConflictExceptionFilter>();
                    options.Filters.Add<DeliveryArchivedExceptionFilter>();

                    if (authConfig.Enabled)
                    {
                        options.Filters.Add<BlockedModeFilter>();
                    }
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.Converters.Add(new API.JsonConverters.UtcDateTimeConverter());
                });

            // Add Swagger services
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.DocumentFilter<LatestRouteFilter>();
            });

            // Add SignalR
            var redisConnectionString = builder.Configuration.GetConnectionString(RedisIdentifier);
            var clusterCoordinationEnabled = !string.IsNullOrWhiteSpace(redisConnectionString);

            var signalRBuilder = builder.Services.AddSignalR()
                 .AddJsonProtocol(options =>
                 {
                     options.PayloadSerializerOptions.Converters
                      .Add(new JsonStringEnumConverter());
                 });

            if (clusterCoordinationEnabled)
            {
                signalRBuilder.AddStackExchangeRedis(redisConnectionString!);
            }

            builder.Services.ConfigureAll<HttpClientFactoryOptions>(o =>
            {
                o.HandlerLifetime = TimeSpan.FromMinutes(2);
            });

            builder.Services
            .AddHttpClient("Default")
            .ConfigurePrimaryHttpMessageHandler(_ => new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
                MaxConnectionsPerServer = 100,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                EnableMultipleHttp2Connections = true
            });

            builder.Services.AddHttpClient(JiraOAuthProvider.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHttpClient(AdoOAuthProvider.HttpClientName, client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        private static readonly char[] AllowedOriginsSeparators = [',', ';'];

        private static AuthenticationConfiguration LoadAuthenticationConfiguration(WebApplicationBuilder builder)
        {
            var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfiguration>()
                ?? new AuthenticationConfiguration();

            if (authConfig.AllowedOrigins.Count > 0)
            {
                return authConfig;
            }

            // Environment-variable provider binds List<string> only via indexed keys (__0, __1, ...).
            // Operators routinely set the scalar Authentication__AllowedOrigins=value; recover that form here.
            var scalar = builder.Configuration["Authentication:AllowedOrigins"];
            if (string.IsNullOrWhiteSpace(scalar))
            {
                return authConfig;
            }

            var origins = scalar
                .Split(AllowedOriginsSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();

            return authConfig with { AllowedOrigins = origins };
        }

        private const string OAuthStateSecretConfigKey = "Lighthouse:OAuth:StateSecret";
        private const string DataProtectionKeyStorePathConfigKey = "Lighthouse:DataProtection:KeyStorePath";
        private const string EncryptionKeyStorePathConfigKey = "Encryption:KeyStorePath";
        private const string DatabaseProviderConfigKey = "Database:Provider";
        private const string DatabaseConnectionStringConfigKey = "Database:ConnectionString";
        private const string PostgresqlProviderName = "postgresql";
        private const string PostgresProviderName = "postgres";
        private const string SqliteProviderName = "sqlite";
        private const string LegacyKeyStoreDirectoryName = "data-protection-keys";
        private const string OAuthStateSecretProtectorPurpose = "Lighthouse.OAuth.StateSecret.v1";
        private const string OAuthStateSecretBlobFileName = "oauth-state-secret.protected";
        private const int OAuthStateSecretByteLength = 32;

        // An embed cookie is otherwise believed on sight, leaving live frames running for the rest of the
        // window after the key or the person behind them is gone - making "delete them" advice that is not
        // true when an administrator reaches for it. Whoever the cookie names is re-resolved on every
        // request instead.
        private static async Task RejectEmbedPrincipalWhoseIdentityIsGone(
            Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext context)
        {
            if (!await EmbedPrincipalStillResolvesAsync(context))
            {
                context.RejectPrincipal();
            }
        }

        private static async Task<bool> EmbedPrincipalStillResolvesAsync(
            Microsoft.AspNetCore.Authentication.Cookies.CookieValidatePrincipalContext context)
        {
            var principal = context.Principal;
            var services = context.HttpContext.RequestServices;

            var subject = principal?.FindFirst(ApiKeyPrincipalFactory.SubjectClaimType)?.Value;
            if (string.IsNullOrWhiteSpace(subject))
            {
                // A principal naming nobody must not fall through open.
                return false;
            }

            // A read-only port on purpose. GetOrCreateFromPrincipalAsync would re-create the
            // profile an administrator has just deleted, on that person's very next request.
            var profile = await services.GetRequiredService<IUserProfileLookup>()
                .FindBySubjectAsync(subject, context.HttpContext.RequestAborted);

            return profile is not null;
        }

        internal static void EnsureOAuthStateSecret(WebApplicationBuilder builder, KeyStoreLocation keyStore)
        {
            var existing = builder.Configuration[OAuthStateSecretConfigKey];
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return;
            }

            var resolvedSecret = ResolveOrCreateProtectedOAuthStateSecret(keyStore.Directory);

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OAuthStateSecretConfigKey] = resolvedSecret,
            });
        }

        internal static void EnsureEncryptionKeyRing(WebApplicationBuilder builder, KeyStoreLocation keyStore)
        {
            ArgumentNullException.ThrowIfNull(keyStore);

            var fileSystem = new PhysicalKeyStoreFileSystem();

            using var keyStoreProtection = DataProtectionKeptIn(keyStore.Directory);

            var ring = new EncryptionKeyRingBootstrapper(
                new ConfiguredKeyRingSource(
                    builder.Configuration[ConfiguredKeyRingSource.RingSettingKey],
                    builder.Configuration[ConfiguredKeyRingSource.SingleKeySettingKey],
                    builder.Configuration[ConfiguredKeyRingSource.RetiredSingleKeySettingKey]),
                new MountedFileKeyRingSource(
                    builder.Configuration[MountedFileKeyRingSource.PathSettingKey], fileSystem),
                new GeneratedKeyRingStore(
                    keyStore.Directory,
                    keyStoreProtection.GetRequiredService<IDataProtectionProvider>(),
                    fileSystem,
                    TimeProvider.System),
                keyStore,
                new DatabaseSecretPresenceProbe(() => DatabaseConnectionFor(builder)),
                new DatabaseSecretReadabilityProbe(() => DatabaseConnectionFor(builder)),
                builder.Configuration.GetValue<bool>(EncryptionKeyRingBootstrapper.StartAnywaySettingKey))
                .Resolve();

            // The ring is registered as a singleton and never written back into configuration: every value
            // in there is reachable from its debug view and from anything that enumerates a section, which
            // is the last place a key belongs
            builder.Services.AddSingleton<IEncryptionKeyRingHolder>(new EncryptionKeyRingHolder(ring));

            // Whether this instance can make a key at all is decided here, once, from where the key in force
            // came from - and an instance that cannot is handed something that refuses rather than something
            // that works. Anywhere the key was supplied, a key made here would be written to a place that
            // loses to the supplied one on the next start, and everything moved onto it would be out of
            // reach. Moving the stored secrets is offered in every deployment; only the making is not.
            builder.Services.AddSingleton<IKeyRingMinter>(ring.CanMint
                ? new GeneratedKeyRingMinter(keyStore.Directory, fileSystem, TimeProvider.System)
                : new AKeyOnlyItsOwnerCanReplace(ring.Custody));

            builder.Services.AddSingleton<OneSecretPassAtATime>();
            builder.Services.AddScoped<ISecretCustodyService, SecretCustodyService>();
            builder.Services.AddScoped<ISecretCustodyReader>(services => services.GetRequiredService<ISecretCustodyService>());
            builder.Services.AddScoped<IPublishedKeySecretCount, PublishedKeySecretCount>();
            builder.Services.AddScoped<IReferencedKeyIds, ReferencedKeyIds>();
            builder.Services.AddScoped<IReadableSecretsNotOnTheActiveKey, ReadableSecretsNotOnTheActiveKey>();
            builder.Services.AddScoped<IStoredSecretSummary, StoredSecretSummaryReader>();

            builder.Services.AddSingleton(new KeyCustodyDescription(
                WhoseKeyThisIs.AndWhereItIsKept(ring.Custody, keyStore.Directory)));

            WatchTheMountedKeysFile(builder, ring);
        }

        // Only where the mounted file is the source that actually answered is there anything to re-read.
        // Naming a file is not the same as being run from one: configuration comes first in the ordering, and
        // an instance given a key both ways is running on the configured one. Re-reading the file there would
        // hand it the other key thirty seconds after a start that had already decided against it, take every
        // credential written under the configured key out of reach, and hand them back on the next restart
        // while taking away whatever had been written in between. The ordering is decided once, in Resolve,
        // and this is the other place that has to agree with it.
        private static void WatchTheMountedKeysFile(WebApplicationBuilder builder, EncryptionKeyRing ring)
        {
            if (ring.Custody != KeyCustody.SuppliedByExternalSecret)
            {
                return;
            }

            var keysFilePath = builder.Configuration[MountedFileKeyRingSource.PathSettingKey];

            if (string.IsNullOrWhiteSpace(keysFilePath))
            {
                return;
            }

            var interval = KeyRingFileWatcher.IntervalFrom(
                builder.Configuration.GetValue<int?>(KeyRingFileWatcher.IntervalSettingKey));

            builder.Services.AddHostedService(services => new KeyRingFileWatcher(
                new MountedFileKeyRingSource(keysFilePath, new PhysicalKeyStoreFileSystem()),
                services.GetRequiredService<IEncryptionKeyRingHolder>(),
                TimeProvider.System,
                interval,
                services.GetRequiredService<ILogger<KeyRingFileWatcher>>()));
        }

        // Asked for lazily, and only on the one path that has nowhere durable to keep a key: every other
        // deployment starts without the database having to be reachable at all.
        private static DbConnection DatabaseConnectionFor(WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration[DatabaseConnectionStringConfigKey] ?? string.Empty;
            var provider = builder.Configuration[DatabaseProviderConfigKey];

            // Every name Lighthouse knows is spelled out, and anything else stops startup here. Handing an
            // unknown name to one of the providers anyway means a misspelt "postgres" is answered with a
            // complaint about a SQLite keyword, which names neither the setting that is wrong nor the fact
            // that a setting is wrong at all.
            return provider?.ToLowerInvariant() switch
            {
                PostgresqlProviderName or PostgresProviderName => new NpgsqlConnection(connectionString),
                SqliteProviderName => new SqliteConnection(connectionString),
                _ => throw new InvalidOperationException(UnrecognisedDatabaseProvider(provider)),
            };
        }

        // The connection string is deliberately left out: it carries the database password on most
        // deployments, and a message printed at startup is the one most likely to be pasted in public.
        private static string UnrecognisedDatabaseProvider(string? provider)
        {
            var given = string.IsNullOrWhiteSpace(provider) ? "not set" : $"set to '{provider}'";

            return $"{DatabaseProviderConfigKey} is {given}. Lighthouse can only use " +
                $"{PostgresqlProviderName}, {PostgresProviderName} or {SqliteProviderName}.";
        }

        // Standalone initialisation writes the database path and the key store path that the resolution
        // below then reads. Keeping both in one method is what stops the two from being swapped by someone
        // reading Main, and taking the standalone step as an argument is what lets a test run this exact
        // sequence and fail if it ever is.
        internal static (KeyStoreLocation Location, KeyStoreMigrationOutcome Migration) InitializeKeyStore(
            WebApplicationBuilder builder,
            bool isStandalone,
            Action<WebApplicationBuilder> initializeStandalonePaths)
        {
            if (isStandalone)
            {
                initializeStandalonePaths(builder);
            }

            return ResolveKeyStoreDirectory(builder);
        }

        internal static (KeyStoreLocation Location, KeyStoreMigrationOutcome Migration) ResolveKeyStoreDirectory(
            WebApplicationBuilder builder)
        {
            var location = KeyStoreLocationFor(builder);

            Directory.CreateDirectory(location.Directory);

            var migration = KeyStoreMigration.CarryOverLegacyKeyStore(
                location.Directory,
                Path.Combine(builder.Environment.ContentRootPath, LegacyKeyStoreDirectoryName));

            return (location, migration);
        }

        // Resolution is a pure reading of settings, so asking a second time to describe the key store in the
        // startup banner cannot land on a different answer than the one the key was actually resolved under.
        private static KeyStoreLocation KeyStoreLocationFor(WebApplicationBuilder builder)
        {
            return KeyStoreResolver.Resolve(
                builder.Configuration[EncryptionKeyStorePathConfigKey],
                builder.Configuration[DataProtectionKeyStorePathConfigKey],
                builder.Configuration[DatabaseProviderConfigKey],
                builder.Configuration[DatabaseConnectionStringConfigKey],
                builder.Environment.ContentRootPath);
        }

        private static string ResolveOrCreateProtectedOAuthStateSecret(string keyStoreDir)
        {
            return SecretAlreadyInTheKeyStore(keyStoreDir) ?? SecretMintedIntoTheKeyStore(keyStoreDir);
        }

        private static string? SecretAlreadyInTheKeyStore(string keyStoreDir)
        {
            var blobPath = Path.Combine(keyStoreDir, OAuthStateSecretBlobFileName);

            if (!File.Exists(blobPath))
            {
                return null;
            }

            using var transientServices = DataProtectionKeptIn(keyStoreDir);
            var protector = transientServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(OAuthStateSecretProtectorPurpose);

            return Convert.ToBase64String(protector.Unprotect(File.ReadAllBytes(blobPath)));
        }

        // Two boots sharing a key store can both reach this having found no secret, and only one of them
        // gets to write it. The other reads the winner's back rather than carrying its own, because a
        // sign-in is started under whatever secret one boot holds and finished under whatever the file
        // holds - and the read is made through a data protection provider built here, so it sees the
        // wrapping key the winner may have written a moment ago rather than the ring this boot cached.
        private static string SecretMintedIntoTheKeyStore(string keyStoreDir)
        {
            using var transientServices = DataProtectionKeptIn(keyStoreDir);
            var protector = transientServices.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector(OAuthStateSecretProtectorPurpose);

            var freshSecret = System.Security.Cryptography.RandomNumberGenerator.GetBytes(OAuthStateSecretByteLength);
            var blobPath = Path.Combine(keyStoreDir, OAuthStateSecretBlobFileName);

            if (KeyStoreFile.WriteIfAbsent(blobPath, protector.Protect(freshSecret)))
            {
                return Convert.ToBase64String(freshSecret);
            }

            return SecretAlreadyInTheKeyStore(keyStoreDir)
                ?? throw new InvalidOperationException(
                    $"Another Lighthouse wrote the OAuth state secret to '{blobPath}' while this one was starting, " +
                    "and it was gone again before this one could read it. Start Lighthouse again, and if it keeps " +
                    "happening, make sure only one instance is using this key store.");
        }

        // Both the OAuth state secret and the encryption key ring are resolved at builder time, BEFORE
        // builder.Build(), so no application-wide container exists yet. A transient mini-host pins the
        // data-protection keys to the same on-disk location every boot uses, so what one boot wrote the
        // next boot can still read.
        private static ServiceProvider DataProtectionKeptIn(string keyStoreDirectory)
        {
            return new ServiceCollection()
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoreDirectory))
                .Services
                .BuildServiceProvider();
        }

        private const string UseStubOAuthProviderConfigKey = "Lighthouse:OAuth:UseStubProvider";

        private static void RegisterStubOAuthProviderIfEnabled(WebApplicationBuilder builder)
        {
            var useStub = builder.Configuration[UseStubOAuthProviderConfigKey];
            if (!string.Equals(useStub, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Register the stub under its dedicated key so abstraction-honesty integration tests can
            // exercise a brand-new IOAuthProvider via DI only.
            builder.Services.AddSingleton<IOAuthProvider>(sp =>
            {
                var serviceConfig = sp.GetRequiredService<IServiceConfig>();
                var timeProvider = sp.GetRequiredService<TimeProvider>();
                return new StubOAuthProvider(serviceConfig, timeProvider, AuthenticationMethodKeys.StubOAuth);
            });
            builder.Services.AddSingleton<IOAuthSchemaExtensions>(
                new OAuthSchemaExtensions(new[] { AuthenticationMethodKeys.StubOAuth }));

            // Substitute the stub for every real *.oauth method declared in the schema so Playwright
            // walking-skeleton scenarios can drive jira.oauth (and future ado.oauth) connections
            // without contacting external identity providers.
            var realOAuthKeys = AuthenticationMethodSchema.GetOAuthProviderKeys().ToList();
            foreach (var key in realOAuthKeys)
            {
                builder.Services.AddSingleton<IOAuthProvider>(sp =>
                    new StubOAuthProvider(
                        sp.GetRequiredService<IServiceConfig>(),
                        sp.GetRequiredService<TimeProvider>(),
                        key));
            }
        }

        private static void EnsureOAuthProvidersRegistered(WebApplication app)
        {
            var registry = app.Services.GetRequiredService<IOAuthProviderRegistry>();
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OAuthProvidersStartupCheck");

            var extras = app.Services.GetService<IOAuthSchemaExtensions>()?.ExtraOAuthKeys
                ?? Array.Empty<string>();
            var missingKeys = AuthenticationMethodSchema.GetOAuthProviderKeys()
                .Concat(extras)
                .Where(key => !TryResolveProvider(registry, key))
                .ToList();

            if (missingKeys.Count == 0)
            {
                return;
            }

            var message =
                "OAuth authentication methods declared in AuthenticationMethodSchema have no matching " +
                $"IOAuthProvider registered: [{string.Join(", ", missingKeys)}]. " +
                "Every '*.oauth' key in the schema must have a corresponding IOAuthProvider registration in DI.";
            logger.LogCritical("{Message}", message);
            throw new InvalidOperationException(message);
        }

        private static async Task RunStartupOrphanedFeatureCleanupAsync(WebApplication app)
        {
            using var startupScope = app.Services.CreateScope();
            try
            {
                var cleanup = startupScope.ServiceProvider.GetRequiredService<IOrphanedFeatureCleanupService>();
                var deleted = await cleanup.CleanupAsync();
                app.Logger.LogInformation("Startup cleanup removed {Count} orphaned features", deleted);
            }
#pragma warning disable CA1031 // startup cleanup is non-fatal
            catch (Exception ex)
#pragma warning restore CA1031
            {
                app.Logger.LogWarning(ex, "Startup orphaned-feature cleanup failed (non-fatal)");
            }
        }

        private static bool TryResolveProvider(IOAuthProviderRegistry registry, string key)
        {
            try
            {
                _ = registry.GetByKey(key);
                return true;
            }
            catch (OAuthProviderNotFoundException)
            {
                return false;
            }
        }

        private static void EnsureCorsFailsClosed(WebApplicationBuilder builder, bool isStandalone)
        {
            if (isStandalone)
            {
                return;
            }

            var authConfig = LoadAuthenticationConfiguration(builder);

            if (authConfig.Enabled && authConfig.AllowedOrigins.Count == 0)
            {
                const string message =
                    "Authentication is enabled but Authentication:AllowedOrigins is empty. " +
                    "Populate Authentication:AllowedOrigins with the exact browser-facing origins " +
                    "(scheme + host + port) that are permitted to call the Lighthouse API. " +
                    "Refusing to start with a wildcard CORS policy under authentication.";
                Console.Error.WriteLine($"FATAL: {message}");
                throw new InvalidOperationException(message);
            }
        }

        private static void ConfigureCors(WebApplicationBuilder builder, AuthenticationConfiguration authConfig)
        {
            var isStandalone = Environment.GetEnvironmentVariable("Standalone") == "true";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", corsPolicyBuilder =>
                {
                    if (isStandalone)
                    {
                        corsPolicyBuilder
                            .SetIsOriginAllowed(_ => true)
                            .AllowCredentials();
                    }
                    else if (authConfig.Enabled && authConfig.AllowedOrigins.Count > 0)
                    {
                        corsPolicyBuilder
                            .WithOrigins(authConfig.AllowedOrigins.ToArray())
                            .AllowCredentials();
                    }
                    else
                    {
                        corsPolicyBuilder.AllowAnyOrigin();
                    }

                    corsPolicyBuilder
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
        }

        private static void ConfigureAuthentication(WebApplicationBuilder builder, AuthenticationConfiguration authConfig)
        {
            if (!authConfig.Enabled)
            {
                builder.Services.AddAuthorizationBuilder();
                builder.Services
                    .AddAuthentication(DisabledAuthenticationHandler.SchemeName)
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DisabledAuthenticationHandler>(
                        DisabledAuthenticationHandler.SchemeName,
                        _ => { });
                return;
            }

            // Add a fallback authorization policy that requires authenticated users by default.
            // Individual controllers/endpoints can opt out with [AllowAnonymous].
            builder.Services.AddAuthorizationBuilder()
                .SetFallbackPolicy(new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build());

            // Skip OIDC middleware registration when essential config values are missing.
            // The AuthModeResolver will still return Misconfigured mode so the frontend
            // can show the appropriate error page without OIDC middleware crashing at runtime.
            if (string.IsNullOrWhiteSpace(authConfig.Authority) || string.IsNullOrWhiteSpace(authConfig.ClientId))
            {
                return;
            }

            var embedConfig = builder.Configuration.GetSection(EmbedConfiguration.SectionName)
                .Get<EmbedConfiguration>() ?? new EmbedConfiguration();

            const string smartScheme = "LighthouseSmartAuth";

            builder.Services.AddAuthentication(options =>
            {
                // Use a forwarding policy scheme as the default so both cookie-based
                // browser sessions and API key requests authenticate correctly.
                options.DefaultScheme = smartScheme;
                options.DefaultChallengeScheme = smartScheme;
                options.DefaultSignInScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddPolicyScheme(smartScheme, "Lighthouse Smart Auth", policyOptions =>
            {
                // Route X-Api-Key to the API key handler and Authorization: Bearer to
                // the JWT bearer handler; everything else (browser sessions, anonymous)
                // goes to the cookie scheme.
                policyOptions.ForwardDefaultSelector = ctx =>
                    SmartAuthSchemeSelector.Select(ctx.Request);
                // Challenges (unauthenticated requests) always flow through the cookie
                // scheme, which in turn handles the API-vs-browser split.
                policyOptions.ForwardChallenge = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                policyOptions.ForwardForbid = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                SmartAuthSchemeSelector.ApiKeyScheme, _ => { })
            .AddJwtBearer(SmartAuthSchemeSelector.JwtBearerScheme, jwtOptions =>
            {
                // Non-browser callers (MCP, CLI tooling) present an IdP access token, validated against
                // the same OIDC authority the browser cookie flow trusts. Claims map through the existing
                // CurrentUserProfileService and RBAC, identical to the cookie principal, so a token and a
                // cookie reach the same permissions. Off unless an authority is configured.
                jwtOptions.Authority = authConfig.Authority;
                jwtOptions.RequireHttpsMetadata = authConfig.RequireHttpsMetadata;
                jwtOptions.MapInboundClaims = false;
                if (!string.IsNullOrWhiteSpace(authConfig.MetadataAddress))
                {
                    jwtOptions.MetadataAddress = authConfig.MetadataAddress;
                }

                jwtOptions.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidateAudience = !string.IsNullOrWhiteSpace(authConfig.Audience),
                    ValidAudience = authConfig.Audience,
                    NameClaimType = "name",
                };
            })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.Name = ".Lighthouse.Session";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(authConfig.SessionLifetimeMinutes);
                options.SlidingExpiration = true;

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 403;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            })
            .AddCookie(SmartAuthSchemeSelector.EmbedCookieScheme, embedOptions =>
            {
                // SameSite=None is what lets Lighthouse render inside another product's iframe, and it
                // is confined to this cookie. The ordinary session cookie above is untouched, and a test
                // asserts .Lighthouse.Session still emits SameSite=Lax.
                embedOptions.Cookie.HttpOnly = true;
                embedOptions.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                embedOptions.Cookie.SameSite = SameSiteMode.None;
                embedOptions.Cookie.Name = SmartAuthSchemeSelector.EmbedCookieName;

                // No first-class Partitioned property exists at net10.0, so the attribute is appended
                // verbatim through CookieBuilder.Extensions.
                embedOptions.Cookie.Extensions.Add("Partitioned");

                embedOptions.ExpireTimeSpan = TimeSpan.FromMinutes(embedConfig.ResolveSessionLifetimeMinutes());
                embedOptions.SlidingExpiration = false;

                embedOptions.Events.OnValidatePrincipal = RejectEmbedPrincipalWhoseIdentityIsGone;

                embedOptions.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };

                embedOptions.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(options =>
            {
                options.Authority = authConfig.Authority;
                options.ClientId = authConfig.ClientId;
                options.ClientSecret = authConfig.ClientSecret;
                options.ResponseType = "code";
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.CallbackPath = authConfig.CallbackPath;
                options.SignedOutCallbackPath = authConfig.SignedOutCallbackPath;
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = authConfig.RequireHttpsMetadata;
                options.MetadataAddress = authConfig.MetadataAddress;

                options.Scope.Clear();
                foreach (var scope in authConfig.Scopes)
                {
                    options.Scope.Add(scope);
                }

                options.Events.OnTokenValidated = WriteGroupSnapshotOnTokenValidatedAsync;
            });
        }

        private static async Task WriteGroupSnapshotOnTokenValidatedAsync(
            Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext context)
        {
            var principal = context.Principal;
            if (principal is null)
            {
                return;
            }

            var stableSubject = principal.FindFirst("sub")?.Value ?? principal.FindFirst("oid")?.Value;
            if (string.IsNullOrWhiteSpace(stableSubject))
            {
                return;
            }

            var authorizationOptions = context.HttpContext.RequestServices
                .GetRequiredService<IOptions<AuthorizationConfiguration>>();
            var groupClaimName = authorizationOptions.Value.GroupClaimName;
            if (string.IsNullOrWhiteSpace(groupClaimName))
            {
                return;
            }

            if (!GroupClaimParser.TryGetGroupValues(principal, groupClaimName, out var groupValues, out var unsupportedFormat)
                || unsupportedFormat)
            {
                return;
            }

            var writer = context.HttpContext.RequestServices.GetRequiredService<IOidcGroupSnapshotWriter>();
            var logger = context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("OidcGroupSnapshotHook");

            try
            {
                await writer.WriteAsync(stableSubject, groupValues.ToList(), context.HttpContext.RequestAborted);
            }
#pragma warning disable CA1031 // intentional broad catch - hook must never break sign-in
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.LogWarning(
                    ex,
                    "OIDC group-snapshot write failed for subject {Subject}; continuing sign-in.",
                    stableSubject);
            }
        }

        private const string ReadyHealthTag = "ready";
        private const string StartupHealthTag = "startup";
        private static readonly string[] ReadyTags = [ReadyHealthTag];
        private static readonly string[] ReadyAndStartupTags = [ReadyHealthTag, StartupHealthTag];
        private static readonly string[] StartupOnlyTags = [StartupHealthTag];

        private static void ConfigureHealthChecks(WebApplicationBuilder builder)
        {
            var healthChecks = builder.Services.AddHealthChecks()
                .AddCheck<DatabaseConnectivityHealthCheck>("database", tags: ReadyTags)
                .AddCheck<MigrationsAppliedHealthCheck>("migrations", tags: ReadyAndStartupTags)
                .AddCheck<DrainReadinessHealthCheck>("draining", tags: ReadyTags);

            if (!string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString(RedisIdentifier)))
            {
                // Startup-only on purpose. The substrate probe validates deployment-constant invariants
                // such as session-mode pooling, atomic shared-store admission, and advisory-lock reclaim
                // on holder death. It is destructive by design, so it belongs on the startup gate, not on
                // the readiness gate that fires every few seconds for the life of a healthy pod. Readiness
                // keeps the non-destructive database and draining checks instead.
                healthChecks.AddCheck<ClusterSubstrateHealthCheck>("cluster-substrate", tags: StartupOnlyTags);
            }
        }

        private static void ConfigureGracefulShutdown(WebApplicationBuilder builder)
        {
            var shutdownConfig = builder.Configuration
                .GetSection(ShutdownConfiguration.SectionName)
                .Get<ShutdownConfiguration>() ?? new ShutdownConfiguration();

            builder.Services.AddSingleton<IReadinessState, ReadinessState>();
            builder.Services.Configure<HostOptions>(options =>
                options.ShutdownTimeout = TimeSpan.FromSeconds(shutdownConfig.TimeoutSeconds));
            builder.Services.AddHostedService<GracefulShutdownService>();
        }

        internal static void ConfigureDataProtection(WebApplicationBuilder builder, KeyStoreLocation keyStore)
        {
            // Auth cookies and the OIDC correlation cookie are encrypted with Data Protection keys.
            // When more than one replica runs, every pod has to share the same key ring, otherwise a
            // cookie issued by one pod cannot be read by another and the OIDC login round trip fails
            // with a redirect loop. The key ring goes to Redis (the same store the scaling backplane
            // uses) when one is configured, and to the local filesystem otherwise for single instance
            // and standalone. A stable application name keeps the ring name identical on every pod.
            var dataProtection = builder.Services
                .AddDataProtection()
                .SetApplicationName("Lighthouse");

            var redisConnectionString = builder.Configuration.GetConnectionString(RedisIdentifier);
            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keyStore.Directory));
                return;
            }

            // Lazy connect so a not-yet-ready Redis at boot does not crash startup; the key ring is
            // first touched on the first auth request, by which point Redis is up.
            var multiplexer = new Lazy<IConnectionMultiplexer>(
                () => ConnectionMultiplexer.Connect(redisConnectionString));
            dataProtection.PersistKeysToStackExchangeRedis(
                () => multiplexer.Value.GetDatabase(),
                "Lighthouse:DataProtection:Keys");
        }

        private static void MapHealthEndpoints(WebApplication app)
        {
            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
            }).AllowAnonymous();

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyHealthTag),
            }).AllowAnonymous();

            app.MapHealthChecks("/health/startup", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(StartupHealthTag),
            }).AllowAnonymous();
        }

        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            builder.Services.Configure<RateLimitingConfiguration>(
                builder.Configuration.GetSection(RateLimitingConfiguration.SectionName));

            builder.Services.Configure<EmbedConfiguration>(
                builder.Configuration.GetSection(EmbedConfiguration.SectionName));

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = (context, cancellationToken) =>
                {
                    var policyName = context.HttpContext.GetEndpoint()?
                        .Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

                    var snapshot = context.HttpContext.RequestServices
                        .GetRequiredService<IOptionsMonitor<RateLimitingConfiguration>>().CurrentValue;
                    var retryAfterSeconds = ResolveRetryAfterSeconds(snapshot, policyName);
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                    return ValueTask.CompletedTask;
                };

                foreach (var policyName in new[]
                {
                    RateLimitingConfiguration.AuthLoginPolicy,
                    RateLimitingConfiguration.ApiKeysPolicy,
                    RateLimitingConfiguration.BootstrapSystemAdminPolicy,
                    RateLimitingConfiguration.EmbedSessionPolicy,
                })
                {
                    var capturedPolicyName = policyName;
                    options.AddPolicy(capturedPolicyName, httpContext =>
                    {
                        var snapshot = httpContext.RequestServices
                            .GetRequiredService<IOptionsMonitor<RateLimitingConfiguration>>().CurrentValue;

                        if (!snapshot.Policies.TryGetValue(capturedPolicyName, out var policyConfig))
                        {
                            return RateLimitPartition.GetNoLimiter("unconfigured");
                        }

                        var partitionKey = ResolvePartitionKey(httpContext);
                        return RateLimitPartition.GetFixedWindowLimiter(
                            partitionKey,
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = policyConfig.PermitLimit,
                                Window = TimeSpan.FromSeconds(policyConfig.WindowSeconds),
                                QueueLimit = policyConfig.QueueLimit,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                AutoReplenishment = true,
                            });
                    });
                }
            });
        }

        private static string ResolvePartitionKey(HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private static int ResolveRetryAfterSeconds(RateLimitingConfiguration config, string? policyName)
        {
            if (policyName is not null
                && config.Policies.TryGetValue(policyName, out var policyConfig)
                && policyConfig.WindowSeconds > 0)
            {
                return policyConfig.WindowSeconds;
            }

            return 60;
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            // Repos
            builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
            builder.Services.AddScoped<IRepository<Team>, TeamRepository>();
            builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            builder.Services.AddScoped<IRepository<Portfolio>>(services => services.GetRequiredService<IPortfolioRepository>());
            builder.Services.AddScoped<IFeatureRepository, FeatureRepository>();
            builder.Services.AddScoped<IRepository<Feature>>(services => services.GetRequiredService<IFeatureRepository>());
            builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            builder.Services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            builder.Services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            builder.Services.AddScoped<IWorkItemBlockedTransitionRepository, WorkItemBlockedTransitionRepository>();
            builder.Services.AddScoped<IFeatureBlockedTransitionRepository, FeatureBlockedTransitionRepository>();
            builder.Services.AddScoped<IBlockedCountSnapshotRepository, BlockedCountSnapshotRepository>();
            builder.Services.AddScoped<IPercentilesOverTimeSnapshotRepository, PercentilesOverTimeSnapshotRepository>();
            builder.Services.AddScoped<IProcessBehaviorSnapshotRepository, ProcessBehaviorSnapshotRepository>();
            builder.Services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            builder.Services.AddScoped<IRepository<AppSetting>, AppSettingRepository>();
            builder.Services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            builder.Services.AddScoped<IRepository<TerminologyEntry>, TerminologyRepository>();
            builder.Services.AddScoped<IRepository<LicenseInformation>, LicenseInformationRepository>();
            builder.Services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            builder.Services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            builder.Services.AddScoped<IDeliveryMetricSnapshotRepository, DeliveryMetricSnapshotRepository>();
            builder.Services.AddScoped<IRepository<RefreshLog>, RefreshLogRepository>();
            builder.Services.AddScoped<IRepository<UserProfile>, UserProfileRepository>();
            builder.Services.AddScoped<IRepository<ApiKeyPermission>, ApiKeyPermissionRepository>();
            builder.Services.AddScoped<IEmbedSessionTokenRepository, EmbedSessionTokenRepository>();

            // Factories
            builder.Services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            builder.Services.AddScoped<IIssueFactory, IssueFactory>();
            builder.Services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();
            builder.Services.AddScoped<IDemoDataFactory, DemoDataFactory>();

            // Services
            builder.Services.AddScoped<IConfigFileUpdater, ConfigFileUpdater>();
            builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
            builder.Services.AddScoped<IEmbedSessionTokenService, EmbedSessionTokenService>();
            builder.Services.AddScoped<IFileSystemService, FileSystemService>();
            builder.Services.AddScoped<IAppSettingService, AppSettingService>();
            builder.Services.AddScoped<ILighthouseReleaseService, LighthouseReleaseService>();
            builder.Services.AddScoped<IAssemblyService, AssemblyService>();
            builder.Services.AddSingleton<Lighthouse.Backend.Cache.Cache<string, object>>();
            builder.Services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            builder.Services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            builder.Services.AddScoped<IPercentilesOverTimeSeriesQuery, PercentilesOverTimeSeriesQuery>();
            builder.Services.AddScoped<IProcessBehaviorSeriesQuery, ProcessBehaviorSeriesQuery>();
            builder.Services.AddScoped<IForecastService, ForecastService>();
            builder.Services.AddScoped<IFeaturePositionMap, FeaturePositionMap>();
            builder.Services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            builder.Services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.Dependencies.IDependencyReconciler, Lighthouse.Backend.Services.Implementation.Dependencies.DependencyReconciler>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.Dependencies.IDependencyHonourPolicy, Lighthouse.Backend.Services.Implementation.Dependencies.DependencyHonourPolicy>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.Dependencies.IDependencyDecision, Lighthouse.Backend.Services.Implementation.Dependencies.DependencyDecision>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.Dependencies.IWhatTheForecastWaitsFor, Lighthouse.Backend.Services.Implementation.Dependencies.WhatTheForecastWaitsFor>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.Dependencies.IDependencyRefreshReporter, Lighthouse.Backend.Services.Implementation.Dependencies.DependencyRefreshReporter>();
            builder.Services.AddScoped<IFeatureRankSeeder, FeatureRankSeeder>();
            builder.Services.AddScoped<IFeatureRankingService, FeatureRankingService>();
            builder.Services.AddScoped<IFeatureMoveAuthorization, FeatureMoveAuthorization>();
            builder.Services.AddScoped<ITeamDataService, TeamDataService>();
            builder.Services.AddScoped<IWorkItemService, WorkItemService>();
            builder.Services.AddScoped<ITerminologyService, TerminologyService>();
            builder.Services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            builder.Services.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
            builder.Services.AddScoped<ILicenseService, LicenseService>();
            builder.Services.AddScoped<IRefreshLogService, RefreshLogService>();
            builder.Services.AddScoped<ILicenseVerifier, LicenseVerifier>();
            builder.Services.AddScoped<IDemoDataService, DemoDataService>();
            builder.Services.AddScoped<IDeliveryRuleService, DeliveryRuleService>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.WorkItemRules.IRuleEvaluator<WorkItem>, Lighthouse.Backend.Services.Implementation.WorkItemRules.RuleEvaluator<WorkItem>>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.WorkItemRules.IRuleFieldProvider<WorkItem>, Lighthouse.Backend.Services.Implementation.WorkItemRules.WorkItemFieldProvider>();
            builder.Services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.WorkItems.IBlockedItemService, Lighthouse.Backend.Services.Implementation.WorkItems.BlockedItemService>();
            builder.Services.AddScoped<IWriteBackService, WriteBackService>();
            builder.Services.AddScoped<IWriteBackTriggerService, WriteBackTriggerService>();
            // One instance for the whole application: it is how the update queue tells the collector in a
            // scope which refresh round that scope is working for, and the queue itself lives that long.
            builder.Services.AddSingleton<WriteBackRoundContext>();
            builder.Services.AddScoped<IWriteBackCollector, WriteBackCollector>();

            builder.Services.AddScoped<IAzureDevOpsWorkTrackingConnector, AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<IJiraWorkTrackingConnector, JiraWorkTrackingConnector>();
            builder.Services.AddScoped<ILinearWorkTrackingConnector, LinearWorkTrackingConnector>();
            builder.Services.AddScoped<CsvWorkTrackingConnector>();
            builder.Services.AddScoped<IServiceNowWorkTrackingConnector, ServiceNowWorkTrackingConnector>();

            // Seeding Services - Register in order they should run
            builder.Services.AddScoped<ISeeder, AppSettingSeeder>();
            builder.Services.AddScoped<ISeeder, InstallTimestampSeeder>();
            builder.Services.AddScoped<ISeeder, OptionalFeatureSeeder>();
            builder.Services.AddScoped<ISeeder, TerminologySeeder>();
            builder.Services.AddScoped<ISeeder, RefreshLogSeeder>();
            builder.Services.AddScoped<ISeeder, ApiKeyOwnerReconciliationSeeder>();

            // Background Services
            builder.Services.AddHostedService<TeamUpdater>();
            builder.Services.AddSingleton<ITeamUpdater, TeamUpdater>();

            builder.Services.AddSingleton<IForecastUpdater, ForecastUpdater>();

            builder.Services.AddHostedService<PortfolioUpdater>();
            builder.Services.AddSingleton<IPortfolioUpdater, PortfolioUpdater>();

            builder.Services.AddSingleton<IOrphanedFeatureCleanupService, OrphanedFeatureCleanupService>();

            builder.Services.AddSingleton<ICryptoService, CryptoService>();

            // Bug #5567: the zone resolves HERE, at builder time, so an unresolvable configured id
            // stops the boot instead of silently degrading to UTC.
            var serviceConfig = new ServiceConfig(builder.Configuration);
            builder.Services.AddSingleton<IServiceConfig>(serviceConfig);
            builder.Services.AddSingleton(TimeProvider.System);

            var instanceTimeZone = LighthouseClock.ResolveInstanceTimeZone(serviceConfig.TimeZone);
            Log.Information("Instance calendar day is anchored on time zone {InstanceTimeZone}", instanceTimeZone.Id);
            builder.Services.AddSingleton<ILighthouseClock>(
                sp => new LighthouseClock(instanceTimeZone, sp.GetRequiredService<TimeProvider>()));
            builder.Services.AddSingleton<IOAuthStateTokenIssuer, OAuthStateTokenIssuer>();
            builder.Services.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();

            var stubModeEnabled = string.Equals(
                builder.Configuration[UseStubOAuthProviderConfigKey],
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!stubModeEnabled)
            {
                builder.Services.AddSingleton<IOAuthProvider>(sp =>
                {
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var timeProvider = sp.GetRequiredService<TimeProvider>();
                    var providerLogger = sp.GetRequiredService<ILogger<JiraOAuthProvider>>();
                    return new JiraOAuthProvider(httpClientFactory.CreateClient(JiraOAuthProvider.HttpClientName), timeProvider, providerLogger);
                });

                builder.Services.AddSingleton<IOAuthProvider>(sp =>
                {
                    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                    var timeProvider = sp.GetRequiredService<TimeProvider>();
                    var providerLogger = sp.GetRequiredService<ILogger<AdoOAuthProvider>>();
                    return new AdoOAuthProvider(httpClientFactory.CreateClient(AdoOAuthProvider.HttpClientName), timeProvider, providerLogger);
                });
            }

            RegisterStubOAuthProviderIfEnabled(builder);

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<IRepository<OAuthCredential>, OAuthCredentialRepository>();
            builder.Services.AddScoped<IOAuthService, OAuthService>();
            builder.Services.AddScoped<IOAuthHealthAggregator, OAuthHealthAggregator>();
            builder.Services.AddScoped<PatAuthStrategy>();
            builder.Services.AddScoped<JiraCloudBasicAuthStrategy>();
            builder.Services.AddScoped<LinearApiKeyAuthStrategy>();
            builder.Services.AddScoped<ServiceNowBasicAuthStrategy>();
            builder.Services.AddScoped<NoOpAuthStrategy>();
            builder.Services.AddScoped<OAuthBearerAuthStrategy>();
            builder.Services.AddScoped<IWorkTrackingAuthStrategyFactory, WorkTrackingAuthStrategyFactory>();
            builder.Services.AddSingleton<IGitHubService, GitHubService>();
            builder.Services.AddSingleton<IRandomNumberService, RandomNumberService>();
            builder.Services.AddSingleton<IDrawStreamFactory, DrawStreamFactory>();
            builder.Services.AddSingleton(ForecastSimulationLimits.Default);
            builder.Services.AddSingleton<IPlatformService, PlatformService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();

            var updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            builder.Services.AddSingleton(updateStatuses);

            var redisConnectionString = builder.Configuration.GetConnectionString(RedisIdentifier);
            if (!string.IsNullOrWhiteSpace(redisConnectionString))
            {
                builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
                builder.Services.AddSingleton<IUpdateStatusStore, RedisUpdateStatusStore>();
                builder.Services.AddSingleton<IUpdateExecutionLock, PostgresUpdateExecutionLock>();
                builder.Services.AddSingleton<IUpdateCompletionNotifier, RedisUpdateCompletionNotifier>();
            }
            else
            {
                builder.Services.AddSingleton<IUpdateStatusStore, InProcessUpdateStatusStore>();
                builder.Services.AddSingleton<IUpdateExecutionLock, InProcessUpdateExecutionLock>();
                builder.Services.AddSingleton<IUpdateCompletionNotifier, InProcessUpdateCompletionNotifier>();
            }

            builder.Services.AddSingleton<UpdateSubstrate>();
            builder.Services.AddSingleton<IUpdateQueueService, UpdateQueueService>();

            builder.Services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
            builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, PortfolioFeaturesRefreshedMetricsInvalidationHandler>();
            builder.Services.AddScoped<IDomainEventHandler<BlackoutConfigurationChanged>, BlackoutConfigurationChangedMetricsInvalidationHandler>();
            builder.Services.AddScoped<DeliveryMetricValuesProjector>();
            builder.Services.AddScoped<IDomainEventHandler<PortfolioForecastsUpdated>, DeliveryMetricSnapshotRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, TeamDataRefreshedForecastTriggerHandler>();
            builder.Services.AddScoped<IDomainEventHandler<FeatureOrderingPolicyChanged>, FeatureOrderingPolicyChangedForecastTriggerHandler>();
            builder.Services.AddScoped<IDomainEventHandler<FeatureRankChanged>, FeatureRankChangedForecastTriggerHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, BlockedCountSnapshotRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, BlockedCountSnapshotRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, PercentilesOverTimeRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, PercentilesOverTimeRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, ProcessBehaviorRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, ProcessBehaviorRecordingHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, DemoBlockedHistoryBackfillHandler>();
             builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, DemoBlockedHistoryBackfillHandler>();
             builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, DemoPercentilesBackfillHandler>();
             builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, DemoPercentilesBackfillHandler>();
            builder.Services.AddScoped<IDomainEventHandler<TeamDeleted>, TeamDeletedRefreshLogCleanupHandler>();
            builder.Services.AddScoped<IDomainEventHandler<TeamDeleted>, TeamDeletedForecastRetriggerHandler>();
            builder.Services.AddScoped<IDomainEventHandler<WorkItemBlocked>, WorkItemBlockedTransitionCaptureHandler>();
            builder.Services.AddScoped<IDomainEventHandler<WorkItemUnblocked>, WorkItemBlockedTransitionCloseHandler>();
            builder.Services.AddScoped<IDomainEventHandler<FeatureBlocked>, FeatureBlockedTransitionCaptureHandler>();
            builder.Services.AddScoped<IDomainEventHandler<FeatureUnblocked>, FeatureBlockedTransitionCloseHandler>();

            // Authentication
            builder.Services.Configure<AuthenticationConfiguration>(builder.Configuration.GetSection("Authentication"));
            builder.Services.Configure<AuthorizationConfiguration>(builder.Configuration.GetSection("Authorization"));
            builder.Services.AddSingleton<IAuthConfigurationValidator, AuthConfigurationValidator>();
            builder.Services.AddScoped<IAuthModeResolver, AuthModeResolver>();
            builder.Services.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();
            builder.Services.AddScoped<IUserProfileLookup, UserProfileLookup>();
            builder.Services.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
            builder.Services.AddScoped<IOidcGroupSnapshotWriter, OidcGroupSnapshotWriter>();

            // Database Management
            builder.Services.AddSingleton<DatabaseMaintenanceGate>();
            builder.Services.AddSingleton<DatabaseOperationTracker>();
            builder.Services.AddSingleton<ICommandRunner, CommandRunner>();

            DatabaseConfigurator.RegisterDatabaseManagementProvider(builder);
            builder.Services.AddSingleton<IDatabaseManagementService, DatabaseManagementService>();
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            DatabaseConfigurator.AddDatabaseConfiguration(builder);
            DatabaseConfigurator.AddDbContext(builder);

            if (builder.Environment.IsEnvironment("Testing"))
            {
                return;
            }

            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            DatabaseConfigurator.ApplyMigrations(scope.ServiceProvider);
            DatabaseConfigurator.SeedDatabase(scope.ServiceProvider);
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            var fileSystemService = new FileSystemService();
            var configFileUpdater = new ConfigFileUpdater(fileSystemService);
            var serilogConfiguration = new SerilogLogConfiguration(builder.Configuration, configFileUpdater, fileSystemService);

            builder.Services.AddSingleton<ILogConfiguration>(serilogConfiguration);

            var logger = LoggingConfigurator.CreateLogger(builder.Configuration, serilogConfiguration.LoggingLevelSwitch);

            Log.Logger = logger;
            builder.Host.UseSerilog(logger, true);
        }

        private static void ConfigureHttps(WebApplicationBuilder builder)
        {
            // Configure Kestrel to use the certificate
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    var certPath = builder.Configuration["Certificate:Path"];
                    var certPassword = builder.Configuration["Certificate:Password"];

                    Log.Information("Using Certificate stored at {CertificatePath}", certPath);

                    if (!string.IsNullOrEmpty(certPath) && File.Exists(certPath))
                    {
                        httpsOptions.ServerCertificate = X509CertificateLoader.LoadPkcs12FromFile(certPath, certPassword);
                    }
                });
            });
        }

        private static void PrintSystemInfo(WebApplication app, WebApplicationBuilder builder)
        {
            var logo = new[]
            {
                "           -----------------------------------           ",
                "         ---------------------------------------         ",
                "        ------------##:--------------------------        ",
                "       -------------#------------------------#----       ",
                "       ------------###-----------------#######----       ",
                "       ----------#######----------############----       ",
                "       ---------------------##################----       ",
                "       ----------##+##-#-#####################----       ",
                "       ----------------------:################----       ",
                "       ---------#########----------###########----       ",
                "       ----------*******----------------:#####:---       ",
                "       ----------#####:#--------------------------       ",
                "       ----------#######--------------------------       ",
                "       ---------:###------------------------------       ",
                "       --------------####-------------------------       ",
                "       ----------########-------------------------       ",
                "       ---------######-##:------------------------       ",
                "       --------:######*##+------------------------       ",
                "       --------######:----------------------------       ",
                "       --------#=---------------------------------       ",
                "       *-----###############---------------------%       ",
                "         ---------------------------------------         ",
                "           -----------------------------------           "
            };

            var ringInForce = app.Services.GetRequiredService<IEncryptionKeyRingHolder>().Current;

            var suppliedRing = builder.Configuration[ConfiguredKeyRingSource.RingSettingKey];
            var suppliedKey = builder.Configuration[ConfiguredKeyRingSource.SingleKeySettingKey];
            var suppliedUnderTheRetiredName = builder.Configuration[ConfiguredKeyRingSource.RetiredSingleKeySettingKey];
            var keysFilePath = builder.Configuration[MountedFileKeyRingSource.PathSettingKey];

            var info = StartupBanner.BuildInfoLines(new StartupBannerFacts(
                Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
                [.. app.Urls.Select(AsSeenFromTheMachineItRunsOn)],
                builder.Configuration.GetValue<string>(DatabaseProviderConfigKey) ?? "Unknown",
                TryGetLogFilePath(builder.Configuration),
                builder.Configuration,
                ringInForce,
                KeyStoreLocationFor(builder),
                ConfiguredKeyRingSource.AnsweredByTheRetiredName(
                    suppliedRing, suppliedKey, suppliedUnderTheRetiredName),
                builder.Configuration.GetValue<bool>(EncryptionKeyRingBootstrapper.StartAnywaySettingKey),
                WhereTheKeyCameFrom.Resolve(
                    ringInForce.Custody, suppliedRing, suppliedKey, suppliedUnderTheRetiredName, keysFilePath)));

            var startupBannerBuilder = new StringBuilder();

            int maxLines = Math.Max(logo.Length, info.Count);

            for (int i = 0; i < maxLines; i++)
            {
                var logoLine = i < logo.Length ? logo[i] : new string(' ', 59);
                var infoLine = i < info.Count ? info[i] : "";

                startupBannerBuilder.AppendLine($"{logoLine}    {infoLine}");
            }

            Log.Logger.Information("\n{StartupBanner}", startupBannerBuilder.ToString());
        }

        private static string AsSeenFromTheMachineItRunsOn(string url)
        {
            return url
                .Replace("http://[::]:", "http://localhost:", StringComparison.Ordinal)
                .Replace("https://[::]:", "https://localhost:", StringComparison.Ordinal)
                .Replace("http://0.0.0.0:", "http://localhost:", StringComparison.Ordinal)
                .Replace("https://0.0.0.0:", "https://localhost:", StringComparison.Ordinal);
        }

        private static string? TryGetLogFilePath(ConfigurationManager configuration)
        {
            try
            {
                // Try to get the file path from Serilog configuration
                var writeTo = configuration.GetSection("Serilog:WriteTo");
                foreach (var sink in writeTo.GetChildren())
                {
                    var name = sink.GetValue<string>("Name");
                    if (name == "File")
                    {
                        var args = sink.GetSection("Args");
                        var path = args.GetValue<string>("path");
                        if (!string.IsNullOrEmpty(path))
                        {
                            return Path.GetDirectoryName(Path.GetFullPath(path));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not retrieve log file path from configuration");
            }

            return null;
        }
    }
}
