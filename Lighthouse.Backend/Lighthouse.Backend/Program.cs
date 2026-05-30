using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.OAuth;
using Lighthouse.Backend.Services.Implementation.OAuth.Providers;
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
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.OAuth;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Settings.Configuration;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
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
using Lighthouse.Backend.Startup;
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

                if (isStandalone)
                {
                    StandaloneInitializer.InitializePaths(builder);
                }

                EnsureOAuthStateSecret(builder);

                ConfigureLogging(builder);
                Log.Information("Starting up Lighthouse!");
                Log.Information("Mode: {Mode}", isStandalone ? "Standalone (Tauri)" : "Server (ASP.NET Core)");

                RegisterServices(builder);

                if (!isStandalone)
                {
                    ConfigureHttps(builder);
                }

                ConfigureServices(builder);
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

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                // Don't cache index.html to ensure users always get the latest version, but allow caching for other static assets
                OnPrepareResponse = ctx =>
                {
                    if (ctx.File.Name == "index.html")
                    {
                        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
                        ctx.Context.Response.Headers.Pragma = "no-cache";
                        ctx.Context.Response.Headers.Expires = "0";
                    }
                }
            });

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

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot";
                spa.Options.DefaultPage = "/index.html";
            });
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var authConfig = LoadAuthenticationConfiguration(builder);

            ConfigureCors(builder, authConfig);
            ConfigureForwardedHeaders(builder, authConfig);
            ConfigureAuthentication(builder, authConfig);
            ConfigureRateLimiting(builder);

            builder.Services
                .AddControllers(options =>
                {
                    options.Filters.Add<ConcurrencyConflictExceptionFilter>();

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
            builder.Services.AddSignalR()
                 .AddJsonProtocol(options =>
                 {
                     options.PayloadSerializerOptions.Converters
                      .Add(new JsonStringEnumConverter());
                 });

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
        private const string OAuthStateSecretProtectorPurpose = "Lighthouse.OAuth.StateSecret.v1";
        private const string OAuthStateSecretBlobFileName = "oauth-state-secret.protected";
        private const int OAuthStateSecretByteLength = 32;

        private static void EnsureOAuthStateSecret(WebApplicationBuilder builder)
        {
            var existing = builder.Configuration[OAuthStateSecretConfigKey];
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return;
            }

            var keyStoreDir = ResolveDataProtectionKeyStoreDir(builder);
            Directory.CreateDirectory(keyStoreDir);

            var resolvedSecret = ResolveOrCreateProtectedOAuthStateSecret(keyStoreDir);

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [OAuthStateSecretConfigKey] = resolvedSecret,
            });
        }

        private static string ResolveDataProtectionKeyStoreDir(WebApplicationBuilder builder)
        {
            var configured = builder.Configuration[DataProtectionKeyStorePathConfigKey];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return Path.Combine(builder.Environment.ContentRootPath, "data-protection-keys");
        }

        private static string ResolveOrCreateProtectedOAuthStateSecret(string keyStoreDir)
        {
            // EnsureOAuthStateSecret runs at builder-time, BEFORE builder.Build(), so no app-wide
            // DI container exists yet. Build a transient mini-host that pins the data-protection
            // key ring to the same on-disk location every boot will use, so the protector is
            // deterministic across restarts.
            using var transientServices = new ServiceCollection()
                .AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keyStoreDir))
                .Services
                .BuildServiceProvider();

            var dataProtectionProvider = transientServices.GetRequiredService<IDataProtectionProvider>();
            var protector = dataProtectionProvider.CreateProtector(OAuthStateSecretProtectorPurpose);
            var blobPath = Path.Combine(keyStoreDir, OAuthStateSecretBlobFileName);

            if (File.Exists(blobPath))
            {
                var protectedBytes = File.ReadAllBytes(blobPath);
                var unprotected = protector.Unprotect(protectedBytes);
                return Convert.ToBase64String(unprotected);
            }

            var freshSecret = System.Security.Cryptography.RandomNumberGenerator.GetBytes(OAuthStateSecretByteLength);
            var protectedSecret = protector.Protect(freshSecret);
            File.WriteAllBytes(blobPath, protectedSecret);
            return Convert.ToBase64String(freshSecret);
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

        private static void ConfigureForwardedHeaders(WebApplicationBuilder builder, AuthenticationConfiguration authConfig)
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                    Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                    Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost;

                foreach (var proxy in authConfig.TrustedProxies)
                {
                    if (IPAddress.TryParse(proxy, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }

                foreach (var network in authConfig.TrustedNetworks)
                {
                    var parts = network.Split('/');
                    if (parts.Length == 2
                        && IPAddress.TryParse(parts[0], out var prefix)
                        && int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownIPNetworks.Add(new IPNetwork(prefix, prefixLength));
                    }
                }
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

            const string apiKeyScheme = "LighthouseApiKey";
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
                // Route X-Api-Key requests to the API key handler; everything
                // else (browser sessions, anonymous) goes to the cookie scheme.
                policyOptions.ForwardDefaultSelector = ctx =>
                {
                    return ctx.Request.Headers.ContainsKey("X-Api-Key")
                        ? apiKeyScheme
                        : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                };
                // Challenges (unauthenticated requests) always flow through the cookie
                // scheme, which in turn handles the API-vs-browser split.
                policyOptions.ForwardChallenge = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                policyOptions.ForwardForbid = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                apiKeyScheme, _ => { })
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

        private static void ConfigureRateLimiting(WebApplicationBuilder builder)
        {
            builder.Services.Configure<RateLimitingConfiguration>(
                builder.Configuration.GetSection(RateLimitingConfiguration.SectionName));

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
            builder.Services.AddScoped<IRepository<Portfolio>, PortfolioRepository>();
            builder.Services.AddScoped<IRepository<Feature>, FeatureRepository>();
            builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            builder.Services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            builder.Services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            builder.Services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            builder.Services.AddScoped<IRepository<AppSetting>, AppSettingRepository>();
            builder.Services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            builder.Services.AddScoped<IRepository<TerminologyEntry>, TerminologyRepository>();
            builder.Services.AddScoped<IRepository<LicenseInformation>, LicenseInformationRepository>();
            builder.Services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            builder.Services.AddScoped<IRepository<RefreshLog>, RefreshLogRepository>();
            builder.Services.AddScoped<IRepository<UserProfile>, UserProfileRepository>();
            builder.Services.AddScoped<IRepository<ApiKeyPermission>, ApiKeyPermissionRepository>();

            // Factories
            builder.Services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            builder.Services.AddScoped<IIssueFactory, IssueFactory>();
            builder.Services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();
            builder.Services.AddScoped<IDemoDataFactory, DemoDataFactory>();

            // Services
            builder.Services.AddScoped<IConfigFileUpdater, ConfigFileUpdater>();
            builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
            builder.Services.AddScoped<IFileSystemService, FileSystemService>();
            builder.Services.AddScoped<IAppSettingService, AppSettingService>();
            builder.Services.AddScoped<ILighthouseReleaseService, LighthouseReleaseService>();
            builder.Services.AddScoped<IAssemblyService, AssemblyService>();
            builder.Services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            builder.Services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
            builder.Services.AddScoped<IForecastService, ForecastService>();
            builder.Services.AddScoped<ITeamDataService, TeamDataService>();
            builder.Services.AddScoped<IWorkItemService, WorkItemService>();
            builder.Services.AddScoped<ITerminologyService, TerminologyService>();
            builder.Services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            builder.Services.AddScoped<ILicenseService, LicenseService>();
            builder.Services.AddScoped<IRefreshLogService, RefreshLogService>();
            builder.Services.AddScoped<ILicenseVerifier, LicenseVerifier>();
            builder.Services.AddScoped<IDemoDataService, DemoDataService>();
            builder.Services.AddScoped<IDeliveryRuleService, DeliveryRuleService>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.WorkItemRules.IRuleEvaluator<WorkItem>, Lighthouse.Backend.Services.Implementation.WorkItemRules.RuleEvaluator<WorkItem>>();
            builder.Services.AddScoped<Lighthouse.Backend.Services.Interfaces.WorkItemRules.IRuleFieldProvider<WorkItem>, Lighthouse.Backend.Services.Implementation.WorkItemRules.WorkItemFieldProvider>();
            builder.Services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
            builder.Services.AddScoped<IWriteBackService, WriteBackService>();
            builder.Services.AddScoped<IWriteBackTriggerService, WriteBackTriggerService>();

            builder.Services.AddScoped<IAzureDevOpsWorkTrackingConnector, AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<IJiraWorkTrackingConnector, JiraWorkTrackingConnector>();
            builder.Services.AddScoped<ILinearWorkTrackingConnector, LinearWorkTrackingConnector>();
            builder.Services.AddScoped<CsvWorkTrackingConnector>();

            // Seeding Services - Register in order they should run
            builder.Services.AddScoped<ISeeder, AppSettingSeeder>();
            builder.Services.AddScoped<ISeeder, OptionalFeatureSeeder>();
            builder.Services.AddScoped<ISeeder, TerminologySeeder>();
            builder.Services.AddScoped<ISeeder, RefreshLogSeeder>();
            builder.Services.AddScoped<ISeeder, ApiKeyOwnerReconciliationSeeder>();

            // Background Services
            builder.Services.AddHostedService<TeamUpdater>();
            builder.Services.AddSingleton<ITeamUpdater, TeamUpdater>();

            builder.Services.AddHostedService<PortfolioUpdater>();
            builder.Services.AddSingleton<IPortfolioUpdater, PortfolioUpdater>();

            builder.Services.AddSingleton<IForecastUpdater, ForecastUpdater>();

            builder.Services.AddSingleton<IOrphanedFeatureCleanupService, OrphanedFeatureCleanupService>();

            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<IServiceConfig, ServiceConfig>();
            builder.Services.AddSingleton(TimeProvider.System);
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
            builder.Services.AddScoped<NoOpAuthStrategy>();
            builder.Services.AddScoped<OAuthBearerAuthStrategy>();
            builder.Services.AddScoped<IWorkTrackingAuthStrategyFactory, WorkTrackingAuthStrategyFactory>();
            builder.Services.AddSingleton<IGitHubService, GitHubService>();
            builder.Services.AddSingleton<IRandomNumberService, RandomNumberService>();
            builder.Services.AddSingleton<IPlatformService, PlatformService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();

            var updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            builder.Services.AddSingleton(updateStatuses);
            builder.Services.AddSingleton<IUpdateQueueService, UpdateQueueService>();

            builder.Services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
            builder.Services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, PortfolioFeaturesRefreshedMetricsInvalidationHandler>();
            builder.Services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, TeamDataRefreshedForecastTriggerHandler>();

            // Authentication
            builder.Services.Configure<AuthenticationConfiguration>(builder.Configuration.GetSection("Authentication"));
            builder.Services.Configure<AuthorizationConfiguration>(builder.Configuration.GetSection("Authorization"));
            builder.Services.AddSingleton<IAuthConfigurationValidator, AuthConfigurationValidator>();
            builder.Services.AddScoped<IAuthModeResolver, AuthModeResolver>();
            builder.Services.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();
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

            var readerOptions = new ConfigurationReaderOptions(
                typeof(FileLoggerConfigurationExtensions).Assembly,
                typeof(ConsoleLoggerConfigurationExtensions).Assembly,
                typeof(Serilog.Templates.ExpressionTemplate).Assembly
            );

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration, readerOptions)
                .MinimumLevel.ControlledBy(serilogConfiguration.LoggingLevelSwitch)
                .Enrich.FromLogContext()
                .CreateLogger();

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

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
            var urls = app.Urls
                .Select(url => url
                    .Replace("http://[::]:", "http://localhost:")
                    .Replace("https://[::]:", "https://localhost:")
                    .Replace("http://0.0.0.0:", "http://localhost:")
                    .Replace("https://0.0.0.0:", "https://localhost:"))
                .ToList();

            var logFilePath = TryGetLogFilePath(builder.Configuration);

            var dbProvider = builder.Configuration.GetValue<string>("Database:Provider") ?? "Unknown";

            string Line(string emoji, string label, string value)
            {
                return $"{emoji}  {label,-13} : {value}";
            }

            var info = new List<string>
            {
                "",
                "",
                "",
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                $"        Lighthouse {version}",
                "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                ""
            };

            info.AddRange(urls.Select(url => Line("🌐", "Url", url)));

            info.Add("");

            info.Add(Line("🖥️", "OS", RuntimeInformation.OSDescription.Trim()));
            info.Add(Line("⚙️", "Runtime", RuntimeInformation.FrameworkDescription));
            info.Add(Line("🧩", "Architecture", RuntimeInformation.OSArchitecture.ToString()));
            info.Add(Line("🔢", "Process ID", Environment.ProcessId.ToString()));
            info.Add(Line("💾", "Database", dbProvider));

            if (!string.IsNullOrEmpty(logFilePath))
            {
                info.Add(Line("📝", "Logs", logFilePath));
            }

            info.AddRange(AuthPostureBanner.BuildAuthPostureLines(builder.Configuration));

            info.Add("");

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