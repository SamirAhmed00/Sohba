using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.HasKey(g => g.Id);
            builder.Property(g => g.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(g => g.Name).IsRequired().HasMaxLength(100);
            builder.Property(g => g.Description).IsRequired().HasMaxLength(1000);
            builder.Property(g => g.Rules).HasMaxLength(2000);

            // Owner/Admin of the group
            builder.HasOne(g => g.Admin)
                   .WithMany(u => u.AdministeredGroups)
                   .HasForeignKey(g => g.AdminId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter for Soft Delete
            builder.HasQueryFilter(g => !g.IsDeleted);
            builder.HasIndex(g => g.IsDeleted);
            builder.HasIndex(g => g.CreatedAt);
        }
    }
}
