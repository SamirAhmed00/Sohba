using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Sohba.Application.DependencyInjection;
using Sohba.Extensions;
using Sohba.Infrastructure.DependencyInjection;
using System;
using System.Text;

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


            builder.Services.AddAuthorization();
            builder.Services.ConfigureApplicationCookie(options => // ptoblem here
            {
                options.LoginPath = "/Auth/Login"; 
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Home/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });

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
            app.UseStaticFiles();
            app.UseRouting();


            app.UseAuthentication();
            app.UseAuthorization();

            
            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                //pattern: "{controller=Home}/{action=Index}/{id?}")
                pattern: "{controller=Landing}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
