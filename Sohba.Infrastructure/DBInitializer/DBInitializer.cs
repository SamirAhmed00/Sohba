using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Infrastructure.Data;

namespace Sohba.Infrastructure.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly AppDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        public DBInitializer(AppDbContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeAsync()
        {
            // Apply migrations
            await _context.Database.MigrateAsync();

            // Seed roles and admin user
            await SeedRolesAsync();
            await SeedAdminUserAsync();
            await SeedSampleDataAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = roleName });
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var userManager = _serviceProvider.GetRequiredService<UserManager<User>>();

            string adminEmail = "admin@sohba.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "Admin User",
                    Bio = "System Administrator",
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123456");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    // Log errors if needed
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }
        }

        private async Task SeedSampleDataAsync()
        {
            if (!_context.Posts.Any())
            {
                var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@sohba.com");
                if (adminUser != null)
                {
                    var newPost = new Domain.Entities.PostAggregate.Post
                    {
                        Title = "Welcome To My Social Media Web APP",
                        Content = "Hello, this is my first post On My Site !",
                        CreatedAt = DateTime.UtcNow,
                        UserId = adminUser.Id,
                        ImageUrl = "https://images.unsplash.com/photo-1586165368502-1bad197a6461?q=80&w=1558&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                    };

                    var newPost2 = new Domain.Entities.PostAggregate.Post
                    {
                        Title = "WebSite Explanation",
                        Content = "Our website is designed to provide a clean, fast, and user-friendly experience across all devices.",
                        CreatedAt = DateTime.UtcNow,
                        UserId = adminUser.Id,
                    };

                    _context.Posts.AddRange(newPost, newPost2);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}