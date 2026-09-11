using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.UserAggregate;
using Sohba.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Primary Key
            builder.HasKey(u => u.Id);

            // Generate GUID on Server side
            builder.Property(u => u.Id)
                   .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(u => u.Name).IsRequired().HasMaxLength(100);

            // Ensure Unique Email
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.Bio).HasMaxLength(500);

       

            // Value conversion: persist enum as string in DB for readability and ASP.NET Identity role compatibility
            builder.Property(u => u.Role)
                   .HasConversion<string>()
                   .HasMaxLength(20)
                   .HasDefaultValue(UserRole.User);

            // Administrator Promotion Lineage Foreign Key
            builder.HasOne(u => u.PromotedByAdminUser)
                   .WithMany()
                   .HasForeignKey(u => u.PromotedByAdminUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // 1. Global Query Filter for Soft Delete
            builder.HasQueryFilter(u => !u.IsDeleted);
            // 2. High-value Indexes (Performance)
            builder.HasIndex(u => u.CreatedAt); // For sorting users by date
            builder.HasIndex(u => u.IsDeleted); // Important for the Global Filter performance
            builder.HasIndex(u => u.Role);
        }
    }
}
