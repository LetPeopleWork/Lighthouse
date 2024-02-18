using Microsoft.EntityFrameworkCore;
using CMFTAspNet.Data;
using CMFTAspNet.Services.Interfaces;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Factories;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<CMFTAspNetContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CMFTAspNetContext") ?? throw new InvalidOperationException("Connection string 'CMFTAspNetContext' not found.")));

builder.Services.AddScoped<IWorkItemServiceFactory, WorkItemServiceFactory>();
builder.Services.AddScoped<IThroughputService, ThroughputService>();

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
