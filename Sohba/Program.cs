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

            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<Sohba.Filters.ValidationFilter>();
            })
            // Configure FluentValidation to automatically validate and populate ModelState
            .AddFluentValidationAutoValidation();

            builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Validators.PostCreateViewModelValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Application.Validators.CommentRequestDtoValidator>();


            builder.Services.AddAuthorization();
            builder.Services.ConfigureApplicationCookie(options => 
            {
                options.LoginPath = "/Auth/Login"; 
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.Cookie.IsEssential = true;
                options.SlidingExpiration = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.HttpOnly = true;
                options.Cookie.MaxAge = null;
                options.Cookie.Name = ".SohbaAuth";
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
