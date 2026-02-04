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
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Settings.Configuration;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using Lighthouse.Backend.macOS;
using Microsoft.Data.Sqlite;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

namespace Lighthouse.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args) ?? throw new ArgumentNullException(nameof(args), "WebApplicationBuilder cannot be null");

            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

                MacInitializer.InitializePaths(builder);

                ConfigureLogging(builder);
                Log.Information("Starting up Lighthouse!");
                Log.Information("Setting Culture Info to {CultureName}", CultureInfo.CurrentCulture.Name);

                MacInitializer.InitializeUpdates();

                RegisterServices(builder);
                ConfigureHttps(builder);
                ConfigureServices(builder);
                ConfigureDatabase(builder);

                var mcpFeature = TryGetOptionalFeature(builder);

                ConfigureOptionalServices(builder, mcpFeature);

                var app = builder.Build();
                ConfigureApp(app, mcpFeature);

                // Start the application in the background
                _ = Task.Run(async () =>
                {
                    await app.StartAsync();

                    // Wait a bit for the server to be fully ready
                    await Task.Delay(500);

                    // Print system info after startup
                    PrintSystemInfo(app, builder);
                });

                // Wait for shutdown signal
                app.WaitForShutdown();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }


        private static OptionalFeature? TryGetOptionalFeature(WebApplicationBuilder builder)
        {
            try
            {
                var serviceProvider = builder.Services.BuildServiceProvider();
                using var scope = serviceProvider.CreateScope();
                var optionalFeatureRepository = scope.ServiceProvider.GetRequiredService<IRepository<OptionalFeature>>();

                // Check if database is accessible
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                if (dbContext.Database.CanConnect())
                {
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

            app.UseCors("AllowAll");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lighthouse API V1");
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
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
                // Only configure MCP server if user has premium license
                var tempServiceProvider = builder.Services.BuildServiceProvider();
                using var scope = tempServiceProvider.CreateScope();
                var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseService>();

                if (licenseService.CanUsePremiumFeatures())
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
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services
                .AddCors(options =>
                {
                    options.AddPolicy("AllowAll",
                      corsPolicyBuilder =>
                      {
                          corsPolicyBuilder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                      });
                })
                .AddControllers().AddJsonOptions(options =>
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
            builder.Services.AddScoped<IDeliveryRepository, DeliveryRepository>();

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
            builder.Services.AddScoped<IProjectMetricsService, ProjectMetricsService>();
            builder.Services.AddScoped<IForecastService, ForecastService>();
            builder.Services.AddScoped<ITeamDataService, TeamDataService>();
            builder.Services.AddScoped<IWorkItemService, WorkItemService>();
            builder.Services.AddScoped<ITerminologyService, TerminologyService>();
            builder.Services.AddScoped<ILicenseService, LicenseService>();
            builder.Services.AddScoped<ILicenseVerifier, LicenseVerifier>();
            builder.Services.AddScoped<IDemoDataService, DemoDataService>();
            builder.Services.AddScoped<IDeliveryRuleService, DeliveryRuleService>();

            builder.Services.AddScoped<IAzureDevOpsWorkTrackingConnector, AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<IJiraWorkTrackingConnector, JiraWorkTrackingConnector>();
            builder.Services.AddScoped<LinearWorkTrackingConnector>();
            builder.Services.AddScoped<CsvWorkTrackingConnector>();

            // MCP Resources
            builder.Services.AddScoped<LighthouseResources>();
            
            // Seeding Services - Register in order they should run
            builder.Services.AddScoped<ISeeder, AppSettingSeeder>();
            builder.Services.AddScoped<ISeeder, OptionalFeatureSeeder>();
            builder.Services.AddScoped<ISeeder, TerminologySeeder>();
            builder.Services.AddScoped<ISeeder, WorkTrackingSystemConnectionSeeder>();

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

            var updateStatuses = new ConcurrentDictionary<UpdateKey, UpdateStatus>();
            builder.Services.AddSingleton(updateStatuses);
            builder.Services.AddSingleton<IUpdateQueueService, UpdateQueueService>();
        }

        private static void ConfigureDatabase(WebApplicationBuilder builder)
        {
            // Configure database settings from appsettings.json
            builder.Services.Configure<DatabaseConfiguration>(
                builder.Configuration.GetSection("Database"));

            // Configure DbContext with options
            builder.Services.AddDbContext<LighthouseAppContext>((provider, options) =>
            {
                var dbConfig = provider.GetRequiredService<IOptions<DatabaseConfiguration>>().Value;
                switch (dbConfig.Provider.ToLower())
                {
                    case "postgresql":
                    case "postgres":
                        options.UseNpgsql(dbConfig.ConnectionString,
                            npgsql =>
                            {
                                npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                npgsql.MigrationsAssembly("Lighthouse.Migrations.Postgres");
                                npgsql.EnableRetryOnFailure(
                                    maxRetryCount: 3,
                                    maxRetryDelay: TimeSpan.FromSeconds(5),
                                    errorCodesToAdd: null);
                                npgsql.CommandTimeout(30);
                            });

                        // Log slow queries in development
                        if (builder.Environment.IsDevelopment())
                        {
                            options.EnableSensitiveDataLogging();
                            options.EnableDetailedErrors();
                        }
                        break;
                    case "sqlite":
                        // Ensure the directory for the SQLite database exists
                        var connectionStringBuilder = new SqliteConnectionStringBuilder(dbConfig.ConnectionString);
                        var dataSource = connectionStringBuilder.DataSource;
                        var directory = Path.GetDirectoryName(dataSource);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        // Create and configure SQLite connection with WAL mode
                        var connection = new SqliteConnection(dbConfig.ConnectionString);
                        connection.Open();

                        // Enable WAL mode and other optimizations
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                    PRAGMA journal_mode=WAL;
                    PRAGMA busy_timeout=10000;
                    PRAGMA synchronous=NORMAL;
                ";
                            command.ExecuteNonQuery();
                        }

                        options.UseSqlite(connection,
                            sqliteOptions =>
                            {
                                sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                sqliteOptions.MigrationsAssembly("Lighthouse.Migrations.Sqlite");
                            });
                        break;
                    default:
                        throw new NotSupportedException($"Database provider '{dbConfig.Provider}' is not supported.");
                }
            });

            if (builder.Environment.IsEnvironment("Testing"))
            {
                return;
            }

            // Run migration
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Migrating Database");
            context.Database.Migrate();
            
            logger.LogInformation("Seeding Database");
            SeedDatabase(scope.ServiceProvider);
        }

        private static void SeedDatabase(IServiceProvider serviceProvider)
        {
            var seeders = serviceProvider.GetServices<ISeeder>();
    
            foreach (var seeder in seeders)
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }
        
        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            var fileSystemService = new FileSystemService();
            var configFileUpdater = new ConfigFileUpdater(fileSystemService);
            var serilogConfiguration = new SerilogLogConfiguration(builder.Configuration, configFileUpdater, fileSystemService);

            builder.Services.AddSingleton<ILogConfiguration>(serilogConfiguration);

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration, new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles))
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