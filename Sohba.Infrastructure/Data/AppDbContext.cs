using Microsoft.EntityFrameworkCore;
using Sohba.Domain.Entities.PostAggregate;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;
using Sohba.Domain.Entities;

namespace Sohba.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Comment> Comments { get; set; }
        public DbSet<Hashtag> Hashtags { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostHashtag> PostHashtags { get; set; }
        public DbSet<PostReport> PostReports { get; set; }
        public DbSet<Reaction> Reactions { get; set; }

        public DbSet<Group> Groups { get; set; }
        public DbSet<GroupMember> GroupMembers { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<PageFollower> PageFollowers { get; set; }

        public DbSet<Friend> Friends { get; set; }  
        public DbSet<User> Users { get; set; }

        public DbSet<Story> Stories { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Make Friends Table Composite Key of UserId and FriendUserId
            modelBuilder.Entity<Friend>()
                .HasKey(f => new { f.UserId, f.FriendUserId });

            modelBuilder.Entity<SavedPost>().HasKey(sp => new { sp.UserId, sp.PostId });
        }
    }
}
