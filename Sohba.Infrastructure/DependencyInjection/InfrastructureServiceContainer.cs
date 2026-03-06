using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Interfaces;
using Sohba.Infrastructure.Data;
using Sohba.Infrastructure.DBInitializer;
using Sohba.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.DependencyInjection
{

    // This Class Is Just A Container For The Extension Method That We Will Use To Register Our Services In The Program.cs 
    // With This Class We Don't Need Any Framework Like Entity Framework To Be Referenced In The Program.cs
    // We Just Need To Reference The Infrastructure Project And Call The Extension Method That We Will Create In This Class
    public static class InfrastructureServiceContainer
    {
        public static IServiceCollection AddInfrastructureService(this IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));


            // Add Identity
            services.AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // We Will Change This TO TRUE After Adding Email Service
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();


            // Register The DBInitializer Service To Be Used In The Program.cs To Initialize The Database With Default Data
            services.AddScoped<IDBInitializer, Sohba.Infrastructure.DBInitializer.DBInitializer>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

    }
}
