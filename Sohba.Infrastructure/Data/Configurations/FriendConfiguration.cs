using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.UserAggregate;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class FriendConfiguration : IEntityTypeConfiguration<Friend>
    {
        public void Configure(EntityTypeBuilder<Friend> builder)
        {
            // Composite Key for Friendship
            builder.HasKey(f => new { f.UserId, f.FriendUserId });

            // Define User side of relationship
            builder.HasOne(f => f.User)
                   .WithMany(u => u.Friends)
                   .HasForeignKey(f => f.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Define Friend side of relationship
            builder.HasOne(f => f.FriendUser)
                   .WithMany()
                   .HasForeignKey(f => f.FriendUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
