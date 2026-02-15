using Sohba.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.DBInitializer
{
    public class DBInitializer : IDBInitializer
    {
        private readonly AppDbContext _context;

        public DBInitializer(AppDbContext context)
        {
            _context = context;
        }
        public async Task InitializeAsync()
        {
            //await ApplyMigrationsAsync();
            //await SeedRolesAsync();
            await SeedUsersAsync();
            //await SeedPostsAsync();
        }

        private async Task SeedUsersAsync()
        {
            // #TODO: We Will Use UserManager and RoleManager to create users and assign roles, but for now, we will just check if users exist in the database and if not, we will create default users.
            // Check if users already exist
            if (!_context.Users.Any() && !_context.Posts.Any())
            {
                var newUser = new Domain.Entities.UserAggregate.User
                {
                    Name = "Admin User",
                    Email = "Samir@Gmail.com",
                    Bio = "Founder Of SOHBA",
                    PasswordHash = "Admin", // In a real application, use a proper password hashing mechanism
                    DateOfBirth = new DateTime(2003, 7, 24),
                    CreatedAt = DateTime.UtcNow,
                    ProfilePictureUrl = "https://images.unsplash.com/photo-1511367461989-f85a21fda167?q=80&w=1631&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync(); 

                var newPost = new Domain.Entities.PostAggregate.Post
                {
                    Title = "Welcome To My Social Media Web APP",
                    Content = "Hello, this is my first post On My Site !",
                    CreatedAt = DateTime.UtcNow,
                    UserId = newUser.Id,
                    ImageUrl = "https://images.unsplash.com/photo-1586165368502-1bad197a6461?q=80&w=1558&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D"
                };


                var newPost2 = new Domain.Entities.PostAggregate.Post
                {
                    Title = "WebSite Explanation",
                    Content = "Our website is designed to provide a clean, fast, and user-friendly experience across all devices. It focuses on delivering clear content, intuitive navigation, and reliable performance to ensure users can easily find what they need. The platform is built with modern web technologies and follows best practices in design, accessibility, and security, making it suitable for both everyday use and future expansion.",
                    CreatedAt = DateTime.UtcNow,
                    UserId = newUser.Id,
                };

                _context.Posts.AddRange(newPost ,newPost2);
                await _context.SaveChangesAsync();

            }
                // If not, create default users (e.g., admin, regular user)
                // Use UserManager to create users and assign roles
        }
    }
}
