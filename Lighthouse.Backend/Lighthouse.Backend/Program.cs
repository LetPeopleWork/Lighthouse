using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.MCP;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Csv;
using Lighthouse.Backend.Services.Implementation.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DatabaseManagement;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.Extensions.Http;
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
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Standalone;

namespace Lighthouse.Backend
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args) ?? throw new ArgumentNullException(nameof(args), "WebApplicationBuilder cannot be null");

            // Check if we are running as a Tauri Sidecar
            var isStandalone = Environment.GetEnvironmentVariable("Standalone") == "true";

            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

                if (isStandalone)
                {
                    StandaloneInitializer.InitializePaths(builder);
                }

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

                var mcpFeature = TryGetOptionalFeature(builder);
                ConfigureOptionalServices(builder, mcpFeature);

                var app = builder.Build();
                ConfigureApp(app, mcpFeature);

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
                Environment.Exit(1); // Force non-zero exit code on failure
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }


        private static OptionalFeature? TryGetOptionalFeature(WebApplicationBuilder builder)
        {
            try
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();

                // Only configure MCP server if user has premium license
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();

                if (!licenseService.CanUsePremiumFeatures())
                {
                    return null;
                }

                // Check if database is accessible
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                if (dbContext.Database.CanConnect())
                {
                    var optionalFeatureRepository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();
                    return optionalFeatureRepository.GetByPredicate(f => f.Key == OptionalFeatureKeys.McpServerKey);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not retrieve MCP feature setting. MCP server will not be configured.");
            }

            return null;
        }

        private static void ConfigureApp(WebApplication app, OptionalFeature? mcpFeature)
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
            app.UseStaticFiles();

            app.UseRouting();
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

            if (mcpFeature?.Enabled ?? false)
            {
                app.MapMcp();
            }
        }

        private static void ConfigureOptionalServices(WebApplicationBuilder builder, OptionalFeature? mcpFeature)
        {
            ConfigureMcpServer(builder, mcpFeature);
        }

        private static void ConfigureMcpServer(WebApplicationBuilder builder, OptionalFeature? mcpFeature)
        {
            if (mcpFeature?.Enabled ?? false)
            {
                builder.Services.AddMcpServer()
                    .WithHttpTransport()
                    .WithTools<LighthouseTeamTools>()
                    .WithTools<LighthouseProjectTools>()
                    .WithTools<LighthouseFeatureTools>()
                    .WithPrompts<LighthousePrompts>()
                    .WithReadResourceHandler(async (request, cancellationToken) =>
                    {
                        var serviceProvider = builder.Services.BuildServiceProvider();
                        using var serviceScope = serviceProvider.CreateScope();
                        var resources = serviceScope.ServiceProvider.GetRequiredService<LighthouseResources>();
                        return await resources.ReadDocumentationResource(request.Params.Uri, cancellationToken);
                    })
                    .WithListResourcesHandler(async (_, _) =>
                    {
                        var serviceProvider = builder.Services.BuildServiceProvider();
                        using var serviceScope = serviceProvider.CreateScope();
                        var resources = serviceScope.ServiceProvider.GetRequiredService<LighthouseResources>();
                        return await resources.ListDocumentationResources();
                    });
            }
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            var authConfig = builder.Configuration.GetSection("Authentication").Get<AuthenticationConfiguration>() ?? new AuthenticationConfiguration();

            ConfigureCors(builder, authConfig);
            ConfigureForwardedHeaders(builder, authConfig);
            ConfigureAuthentication(builder, authConfig);

            builder.Services
                .AddControllers(options =>
                {
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
            builder.Services.AddSwaggerGen();

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

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectDefaults.AuthenticationScheme;
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
            });
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            // Repos
            builder.Services.AddScoped<IRepository<Team>, TeamRepository>();
            builder.Services.AddScoped<IRepository<Portfolio>, PortfolioRepository>();
            builder.Services.AddScoped<IRepository<Feature>, FeatureRepository>();
            builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            builder.Services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            builder.Services.AddScoped<IRepository<AppSetting>, AppSettingRepository>();
            builder.Services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            builder.Services.AddScoped<IRepository<TerminologyEntry>, TerminologyRepository>();
            builder.Services.AddScoped<IRepository<LicenseInformation>, LicenseInformationRepository>();
            builder.Services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();
            builder.Services.AddScoped<IRepository<RefreshLog>, RefreshLogRepository>();

            // Factories
            builder.Services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            builder.Services.AddScoped<IIssueFactory, IssueFactory>();
            builder.Services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();
            builder.Services.AddScoped<IDemoDataFactory, DemoDataFactory>();

            // Services
            builder.Services.AddScoped<IConfigFileUpdater, ConfigFileUpdater>();
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
            builder.Services.AddScoped<IWriteBackService, WriteBackService>();
            builder.Services.AddScoped<IWriteBackTriggerService, WriteBackTriggerService>();

            builder.Services.AddScoped<IAzureDevOpsWorkTrackingConnector, AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<IJiraWorkTrackingConnector, JiraWorkTrackingConnector>();
            builder.Services.AddScoped<ILinearWorkTrackingConnector, LinearWorkTrackingConnector>();
            builder.Services.AddScoped<CsvWorkTrackingConnector>();

            // MCP Resources
            builder.Services.AddScoped<LighthouseResources>();

            // Seeding Services - Register in order they should run
            builder.Services.AddScoped<ISeeder, AppSettingSeeder>();
            builder.Services.AddScoped<ISeeder, OptionalFeatureSeeder>();
            builder.Services.AddScoped<ISeeder, TerminologySeeder>();
            builder.Services.AddScoped<ISeeder, RefreshLogSeeder>();

            // Background Services
            builder.Services.AddHostedService<TeamUpdater>();
            builder.Services.AddSingleton<ITeamUpdater, TeamUpdater>();

            builder.Services.AddHostedService<PortfolioUpdater>();
            builder.Services.AddSingleton<IPortfolioUpdater, PortfolioUpdater>();

            builder.Services.AddSingleton<IForecastUpdater, ForecastUpdater>();

            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<IGitHubService, GitHubService>();
            builder.Services.AddSingleton<IRandomNumberService, RandomNumberService>();
            builder.Services.AddSingleton<IPlatformService, PlatformService>();
            builder.Services.AddSingleton<IProcessService, ProcessService>();
            builder.Services.AddSingleton<ISystemInfoService, SystemInfoService>();

            var updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            builder.Services.AddSingleton(updateStatuses);
            builder.Services.AddSingleton<IUpdateQueueService, UpdateQueueService>();

            // Authentication
            builder.Services.Configure<AuthenticationConfiguration>(builder.Configuration.GetSection("Authentication"));
            builder.Services.AddSingleton<IAuthConfigurationValidator, AuthConfigurationValidator>();
            builder.Services.AddScoped<IAuthModeResolver, AuthModeResolver>();

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