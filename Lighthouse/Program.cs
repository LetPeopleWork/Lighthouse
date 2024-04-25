using Lighthouse.Services.Interfaces;
using Lighthouse.Services.Implementation;
using Lighthouse.Services.Factories;
using Lighthouse.Services.Implementation.Repositories;
using Microsoft.EntityFrameworkCore;
using Lighthouse.Models;
using Lighthouse.Factories;
using Lighthouse.Services.Implementation.BackgroundServices;
using Lighthouse.Data;
using System.Globalization;
using Lighthouse.Services.Implementation.WorkItemServices;

namespace Lighthouse
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.CurrentCulture;

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<LighthouseAppContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("LighthouseAppContext") ?? throw new InvalidOperationException("Connection string 'LighthouseAppContext' not found")));

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
            
            builder.Services.AddHostedService<ThroughputUpdateService>();
            builder.Services.AddHostedService<WorkItemUpdateService>();
            builder.Services.AddHostedService<ForecastUpdateService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
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
    }
}