using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Sohba.Application.DependencyInjection;
using Sohba.Application.Settings;
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

            // ============================================================
            // 1. REGISTER JWT SETTINGS
            // ============================================================
            var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
            if (jwtSettings != null)
            {
                jwtSettings.Validate();
                builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            }

            // ============================================================
            // 2. ADD JWT AUTHENTICATION
            // ============================================================
            var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is missing"));

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false; // Set to true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
            
            // COOKIE AUTH (for MVC views) + JWT (for API calls)
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

            // ============================================================
            // 3. INFRASTRUCTURE & APPLICATION SERVICES
            // ============================================================
            builder.Services.AddInfrastructureService(builder.Configuration);
            builder.Services.AddApplicationServices();

            // ============================================================
            // 4. MVC & VALIDATION
            // ============================================================
            builder.Services.AddControllersWithViews(options =>
            {
                options.Filters.Add<Sohba.Filters.ValidationFilter>();
            });

            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Validators.PostCreateViewModelValidator>();
            builder.Services.AddValidatorsFromAssemblyContaining<Sohba.Application.Validators.CommentRequestDtoValidator>();


            // ============================================================
            // 5. BUILD APP
            // ============================================================
            var app = builder.Build();


            // ============================================================
            // 6. DATABASE INITIALIZATION
            // ============================================================
            await app.InitializeDatabaseAsync();


            // ============================================================
            // 7. MIDDLEWARE PIPELINE
            // ============================================================
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
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
