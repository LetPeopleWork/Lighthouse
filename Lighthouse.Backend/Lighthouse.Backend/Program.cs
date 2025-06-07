using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Microsoft.EntityFrameworkCore;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Data;
using System.Globalization;
using Serilog;
using System.Text.Json.Serialization;
using Serilog.Settings.Configuration;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Update;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.TeamData;
using Lighthouse.Backend.Services.Implementation.TeamData;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Interfaces.WorkItems;
using Lighthouse.Backend.MCP;
using ModelContextProtocol.Protocol.Types;

namespace Lighthouse.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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

                ConfigureOptionalServices(builder);

                var app = builder.Build();
                ConfigureApp(app);
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

            app.UseCors("AllowAll");

            app.UseSwagger(c =>
            {
                c.RouteTemplate = "api/swagger/{documentName}/swagger.json";
            });
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/api/swagger/v1/swagger.json", "Lighthouse API V1");
                c.RoutePrefix = "api/swagger";
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }

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

            app.MapMcp();
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

            builder.Services.AddHttpClient();
        }

        private static void ConfigureOptionalServices(WebApplicationBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var optionalFeatureRepository = serviceProvider.GetRequiredService<IRepository<OptionalFeature>>();

            var mcpFeature = optionalFeatureRepository.GetByPredicate(f => f.Key == OptionalFeatureKeys.McpServerKey);
            ConfigureMcpServer(builder, mcpFeature);
        }

        private static void ConfigureMcpServer(WebApplicationBuilder builder, OptionalFeature? mcpFeature)
        {
            if (mcpFeature?.Enabled ?? false)
            {
                builder.Services.AddMcpServer()
                    .WithTools<LighthouseTeamTools>()
                    .WithGetPromptHandler((request, cancellationToken) => Task.FromResult(new GetPromptResult()))
                    .WithListPromptsHandler((request, cancellationToken) => Task.FromResult(new ListPromptsResult()))
                    .WithReadResourceHandler((request, cancellationToken) => Task.FromResult(new ReadResourceResult()))
                    .WithListResourcesHandler((request, cancellationToken) => Task.FromResult(new ListResourcesResult()));
            }
        }

        private static void RegisterServices(WebApplicationBuilder builder)
        {
            // Repos
            builder.Services.AddScoped<IRepository<Team>, TeamRepository>();
            builder.Services.AddScoped<IRepository<Project>, ProjectRepository>();
            builder.Services.AddScoped<IRepository<Feature>, FeatureRepository>();
            builder.Services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            builder.Services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
            builder.Services.AddScoped<IRepository<AppSetting>, AppSettingRepository>();
            builder.Services.AddScoped<IRepository<FeatureHistoryEntry>, FeatureHistoryRepository>();
            builder.Services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();

            // Factories
            builder.Services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            builder.Services.AddScoped<IIssueFactory, IssueFactory>();
            builder.Services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();

            // Services
            builder.Services.AddScoped<ILexoRankService, LexoRankService>();
            builder.Services.AddScoped<IConfigFileUpdater, ConfigFileUpdater>();
            builder.Services.AddScoped<IFileSystemService, FileSystemService>();
            builder.Services.AddScoped<IAppSettingService, AppSettingService>();
            builder.Services.AddScoped<ILighthouseReleaseService, LighthouseReleaseService>();
            builder.Services.AddScoped<IAssemblyService, AssemblyService>();
            builder.Services.AddScoped<IFeatureHistoryService, FeatureHistoryService>();
            builder.Services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            builder.Services.AddScoped<IProjectMetricsService, ProjectMetricsService>();
            builder.Services.AddScoped<IForecastService, ForecastService>();
            builder.Services.AddScoped<ITeamDataService, TeamDataService>();
            builder.Services.AddScoped<IWorkItemService, WorkItemService>();

            builder.Services.AddScoped<AzureDevOpsWorkTrackingConnector>();
            builder.Services.AddScoped<JiraWorkTrackingConnector>();
            builder.Services.AddScoped<LinearWorkTrackingConnector>();

            // Background Services
            builder.Services.AddHostedService<TeamUpdater>();
            builder.Services.AddSingleton<ITeamUpdater, TeamUpdater>();

            builder.Services.AddHostedService<ProjectUpdater>();
            builder.Services.AddSingleton<IProjectUpdater, ProjectUpdater>();

            builder.Services.AddSingleton<IForecastUpdater, ForecastUpdater>();

            builder.Services.AddHostedService<DataRetentionService>();


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
                            });
                        break;
                    case "sqlite":
                        options.UseSqlite(dbConfig.ConnectionString,
                            options =>
                            {
                                options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                                options.MigrationsAssembly("Lighthouse.Migrations.Sqlite");
                            });
                        break;
                    default:
                        throw new NotSupportedException($"Database provider '{dbConfig.Provider}' is not supported.");
                }
            });

            // Run migration
            using var scope = builder.Services.BuildServiceProvider().CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Migrating Database");
            context.Database.Migrate();
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