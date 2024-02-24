global using AppContext = CMFTAspNet.Data.AppContext;

using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Factories;
using CMFTAspNet.Services.Implementation.Repositories;
using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Models;

namespace CMFTAspNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddDbContext<Data.AppContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("AppContext") ?? throw new InvalidOperationException("Connection string 'AppContext' not found")));


            // Ensure the database is created and migrate it
            using (var serviceScope = builder.Services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetService<AppContext>();
                dbContext.Database.Migrate();
            }

            builder.Services.AddScoped<IRepository<Team>, TeamRepository>();
            builder.Services.AddScoped<IRepository<Project>, ProjectRepository>();
            builder.Services.AddScoped<IRepository<Feature>, FeatureRepository>();
            builder.Services.AddScoped<IRandomNumberService, RandomNumberService>();
            builder.Services.AddScoped<IMonteCarloService, MonteCarloService>();
            builder.Services.AddScoped<IWorkItemServiceFactory, WorkItemServiceFactory>();
            builder.Services.AddScoped<IThroughputService, ThroughputService>();
            builder.Services.AddScoped<IWorkItemCollectorService, WorkItemCollectorService>();

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

            app.MapRazorPages();

            app.Run();
        }
    }
}