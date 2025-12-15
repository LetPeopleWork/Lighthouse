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
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
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
using Microsoft.Data.Sqlite;

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

                ConfigureLogging(builder);
                Log.Information("Starting up Lighthouse!");
                Log.Information("Setting Culture Info to {CultureName}", CultureInfo.CurrentCulture.Name);

                RegisterServices(builder);
                ConfigureHttps(builder);
                ConfigureServices(builder);
                ConfigureDatabase(builder);

                var mcpFeature = TryGetOptionalFeature(builder);

                ConfigureOptionalServices(builder, mcpFeature);

                var app = builder.Build();
                ConfigureApp(app, mcpFeature);
                app.Run();
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
                            using var scope = serviceProvider.CreateScope();
                            var resources = scope.ServiceProvider.GetRequiredService<LighthouseResources>();
                            return await resources.ReadDocumentationResource(request.Params.Uri, cancellationToken);
                        })
                        .WithListResourcesHandler(async (request, cancellationToken) =>
                        {
                            var serviceProvider = builder.Services.BuildServiceProvider();
                            using var scope = serviceProvider.CreateScope();
                            var resources = scope.ServiceProvider.GetRequiredService<LighthouseResources>();
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
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                      });
                })
                .AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
            .ConfigurePrimaryHttpMessageHandler(sp => new SocketsHttpHandler
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
            builder.Services.AddScoped<IRepository<Portfolio>, ProjectRepository>();
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
            builder.Services.AddScoped<ILexoRankService, LexoRankService>();
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

            builder.Services.AddScoped<AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<JiraWorkTrackingConnector>();
            builder.Services.AddScoped<LinearWorkTrackingConnector>();
            builder.Services.AddScoped<CsvWorkTrackingConnector>();

            // MCP Resources
            builder.Services.AddScoped<LighthouseResources>();

            // Background Services
            builder.Services.AddHostedService<TeamUpdater>();
            builder.Services.AddSingleton<ITeamUpdater, TeamUpdater>();

            builder.Services.AddHostedService<ProjectUpdater>();
            builder.Services.AddSingleton<IProjectUpdater, ProjectUpdater>();

            builder.Services.AddSingleton<IForecastUpdater, ForecastUpdater>();

            builder.Services.AddSingleton<ICryptoService, CryptoService>();
            builder.Services.AddSingleton<IGitHubService, GitHubService>();
            builder.Services.AddSingleton<IRandomNumberService, RandomNumberService>();

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

            if (!builder.Environment.IsEnvironment("Testing"))
            {
                // Run migration
                using var scope = builder.Services.BuildServiceProvider().CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Migrating Database");
                context.Database.Migrate();
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
    }
}