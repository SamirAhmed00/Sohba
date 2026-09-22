using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using Sohba.Infrastructure.Data;

namespace Sohba.Infrastructure.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DBInitializer> _logger;

        public DBInitializer(
            AppDbContext context,
            IServiceProvider serviceProvider,
            ILogger<DBInitializer> logger)
        {
            _context = context;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Starting database initialization...");

            // 1. Apply all pending EF Core migrations
            await _context.Database.MigrateAsync();

            _logger.LogInformation("Database migrations completed successfully.");

            // 2. Seed required application roles
            await SeedRolesAsync();

            // 3. Bootstrap the platform Owner account
            await BootstrapOwnerUserAsync();

            _logger.LogInformation("Database initialization completed successfully.");
        }

        private async Task SeedRolesAsync()
        {
            var roleManager =
                _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            string[] roleNames =
            {
                "Owner",
                "Admin",
                "User"
            };

            foreach (var roleName in roleNames)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogInformation(
                        "Role '{RoleName}' already exists.",
                        roleName);

                    continue;
                }

                var result = await roleManager.CreateAsync(
                    new IdentityRole<Guid>
                    {
                        Name = roleName
                    });

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to create role '{roleName}': {errors}");
                }

                _logger.LogInformation(
                    "Role '{RoleName}' created successfully.",
                    roleName);
            }
        }

        private async Task BootstrapOwnerUserAsync()
        {
            var configuration =
                _serviceProvider.GetRequiredService<IConfiguration>();

            var userManager =
                _serviceProvider.GetRequiredService<UserManager<User>>();

            // Configuration priority:
            // Environment variable overrides appsettings.json automatically.
            var ownerEmail =
                configuration["Sohba:Owner:Email"];

            var initialPassword =
                configuration["Sohba:Owner:InitialPassword"];

            if (string.IsNullOrWhiteSpace(ownerEmail))
            {
                throw new InvalidOperationException(
                    "Sohba:Owner:Email must be configured.");
            }

            if (string.IsNullOrWhiteSpace(initialPassword))
            {
                throw new InvalidOperationException(
                    "Sohba:Owner:InitialPassword must be configured.");
            }

            _logger.LogInformation(
                "Bootstrapping Owner account for {OwnerEmail}.",
                ownerEmail);

            var ownerUser =
                await userManager.FindByEmailAsync(ownerEmail);

            if (ownerUser == null)
            {
                ownerUser = new User
                {
                    Id = Guid.NewGuid(),

                    UserName = ownerEmail,
                    Email = ownerEmail,

                    Name = "Sohba Platform Owner",

                    Bio =
                        "Supreme Platform Administrator and Owner",

                    Role = UserRole.Owner,

                    CreatedAt = DateTime.UtcNow,

                    IsActive = true,

                    EmailConfirmed = true,

                    ProfilePictureUrl =
                        "https://ui-avatars.com/api/?name=Owner&background=f59e0b&color=fff&size=128"
                };

                var createResult =
                    await userManager.CreateAsync(
                        ownerUser,
                        initialPassword);

                if (!createResult.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        createResult.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to create Owner user '{ownerEmail}': {errors}");
                }

                _logger.LogInformation(
                    "Owner user '{OwnerEmail}' created successfully.",
                    ownerEmail);
            }
            else
            {
                _logger.LogInformation(
                    "Owner user '{OwnerEmail}' already exists.",
                    ownerEmail);

                // Keep the strongly-typed application role in sync.
                if (ownerUser.Role != UserRole.Owner)
                {
                    ownerUser.Role = UserRole.Owner;

                    var updateResult =
                        await userManager.UpdateAsync(ownerUser);

                    if (!updateResult.Succeeded)
                    {
                        var errors = string.Join(
                            " | ",
                            updateResult.Errors.Select(e => e.Description));

                        throw new InvalidOperationException(
                            $"Failed to update Owner user '{ownerEmail}': {errors}");
                    }

                    _logger.LogInformation(
                        "User '{OwnerEmail}' was updated to Owner.",
                        ownerEmail);
                }
            }

            // Ensure Owner Identity role
            if (!await userManager.IsInRoleAsync(ownerUser, "Owner"))
            {
                var result =
                    await userManager.AddToRoleAsync(
                        ownerUser,
                        "Owner");

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to add 'Owner' role to '{ownerEmail}': {errors}");
                }

                _logger.LogInformation(
                    "Owner role assigned to '{OwnerEmail}'.",
                    ownerEmail);
            }

            // Ensure Admin Identity role
            if (!await userManager.IsInRoleAsync(ownerUser, "Admin"))
            {
                var result =
                    await userManager.AddToRoleAsync(
                        ownerUser,
                        "Admin");

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        " | ",
                        result.Errors.Select(e => e.Description));

                    throw new InvalidOperationException(
                        $"Failed to add 'Admin' role to Owner '{ownerEmail}': {errors}");
                }

                _logger.LogInformation(
                    "Admin role assigned to Owner '{OwnerEmail}'.",
                    ownerEmail);
            }

            _logger.LogInformation(
                "Owner bootstrap completed successfully for '{OwnerEmail}'.",
                ownerEmail);
        }
    }
}