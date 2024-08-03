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
using Lighthouse.Backend.Services.Implementation.WorkItemServices;
using Serilog;
using System.Text.Json.Serialization;
using Serilog.Settings.Configuration;

namespace Lighthouse.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            Log.Information("Starting up Lighthouse!");

            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

                var builder = WebApplication.CreateBuilder(args);

                var fileSystemService = new FileSystemService();
                var configFileUpdater = new ConfigFileUpdater(builder.Configuration, fileSystemService);
                var serilogConfiguration = new SerilogLogConfiguration(builder.Configuration, configFileUpdater, fileSystemService);
                builder.Services.AddSingleton<ILogConfiguration>(serilogConfiguration);

                builder.Services.AddSerilog((services, lc) => lc
                    .ReadFrom.Configuration(builder.Configuration, new ConfigurationReaderOptions(ConfigurationAssemblySource.AlwaysScanDllFiles))
                    .ReadFrom.Services(services)
                    .MinimumLevel.ControlledBy(serilogConfiguration.LoggingLevelSwitch)
                    .Enrich.FromLogContext());

                Log.Information("Setting Culture Info to {CultureName}", CultureInfo.CurrentCulture.Name);

                // Add services to the container.
                builder.Services.AddRazorPages();
                builder.Services.AddDbContext<LighthouseAppContext>(options =>
                    options.UseSqlite(
                        builder.Configuration.GetConnectionString("LighthouseAppContext") ?? throw new InvalidOperationException("Connection string 'LighthouseAppContext' not found"),
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    )
                );

                // Repos
                builder.Services.AddScoped<IRepository<Team>, TeamRepository>();
                builder.Services.AddScoped<IRepository<Project>, ProjectRepository>();
                builder.Services.AddScoped<IRepository<Feature>, FeatureRepository>();
                builder.Services.AddScoped<IRepository<WorkTrackingSystemConnection>, WorkTrackingSystemConnectionRepository>();
                builder.Services.AddScoped<IRepository<AppSetting>, AppSettingRepository>();

                // Factories
                builder.Services.AddScoped<IWorkItemServiceFactory, WorkItemServiceFactory>();
                builder.Services.AddScoped<IIssueFactory, IssueFactory>();
                builder.Services.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();

                // Services
                builder.Services.AddScoped<IRandomNumberService, RandomNumberService>();
                builder.Services.AddScoped<IMonteCarloService, MonteCarloService>();
                builder.Services.AddScoped<IThroughputService, ThroughputService>();
                builder.Services.AddScoped<IWorkItemCollectorService, WorkItemCollectorService>();
                builder.Services.AddScoped<ILexoRankService, LexoRankService>();
                builder.Services.AddScoped<IConfigFileUpdater, ConfigFileUpdater>();
                builder.Services.AddScoped<IFileSystemService, FileSystemService>();
                builder.Services.AddScoped<IAppSettingService, AppSettingService>();
                builder.Services.AddScoped<ILighthouseReleaseService, LighthouseReleaseService>();
                builder.Services.AddScoped<IGitHubService, GitHubService>();

                builder.Services.AddScoped<AzureDevOpsWorkItemService>();
                builder.Services.AddScoped<JiraWorkItemService>();

                builder.Services.AddHostedService<ThroughputUpdateService>();
                builder.Services.AddHostedService<FeatureUpdateService>();
                builder.Services.AddHostedService<ForecastUpdateService>();

                builder.Services.AddSingleton<ICryptoService, CryptoService>();

                // Add CORS services
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

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseCors("AllowAll");
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();

                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthorization();

                app.MapControllers();

                app.UseSpa(spa =>
                {
                    spa.Options.SourcePath = "wwwroot";
                    spa.Options.DefaultPage = "/index.html";
                });

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
    }
}