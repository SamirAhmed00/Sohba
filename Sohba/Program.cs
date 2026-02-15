using Sohba.Extensions;
using Sohba.Infrastructure.DependencyInjection;
using Sohba.Application.DependencyInjection;
using System;

namespace Sohba
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            // Get Connection String From Configuration
            builder.Services.AddInfrastructureService(builder.Configuration);

            // Add Application Services (AutoMapper)
            builder.Services.AddApplicationServices();

            // Add MVC Services
            builder.Services.AddControllersWithViews();

            // Layer registrations
            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructureService(builder.Configuration);


            var app = builder.Build(); // Here

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            await app.InitializeDatabaseAsync();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
