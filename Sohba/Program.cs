using Microsoft.EntityFrameworkCore;
using System;

namespace Sohba
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Get Connection String From Configuration
            //builder.Services.AddDbContext<AppDbContext>(options =>
            //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Clear EF and EF SQL and EF Tools
            // Replace Them With :
            // Ìæå ØÈÞÉ ÇáÜ Infrastructure
                //    public static class InfrastructureRegistration
                //    {
                //    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
                //    {
                //        services.AddDbContext<AppDbContext>(options =>
                //            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
                //        return services;
                //    }
                //}


        var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

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
