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
                builder.Services.AddSerilog((services, lc) => lc
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
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

                // Factories
                builder.Services.AddScoped<IWorkItemServiceFactory, WorkItemServiceFactory>();
                builder.Services.AddScoped<IWorkTrackingOptionsFactory, WorkTrackingOptionsFactory>();
                builder.Services.AddScoped<IIssueFactory, IssueFactory>();

                // Services
                builder.Services.AddScoped<IRandomNumberService, RandomNumberService>();
                builder.Services.AddScoped<IMonteCarloService, MonteCarloService>();
                builder.Services.AddScoped<IThroughputService, ThroughputService>();
                builder.Services.AddScoped<IWorkItemCollectorService, WorkItemCollectorService>();
                builder.Services.AddScoped<ILexoRankService, LexoRankService>();

                builder.Services.AddScoped<AzureDevOpsWorkItemService>();
                builder.Services.AddScoped<JiraWorkItemService>();

                builder.Services.AddHostedService<ThroughputUpdateService>();
                builder.Services.AddHostedService<WorkItemUpdateService>();
                builder.Services.AddHostedService<ForecastUpdateService>();

                // Add CORS services
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll",
                        builder =>
                        {
                            builder.AllowAnyOrigin()
                                   .AllowAnyMethod()
                                   .AllowAnyHeader();
                        });
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
                    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthorization();

                app.MapControllers();
                app.MapRazorPages();

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