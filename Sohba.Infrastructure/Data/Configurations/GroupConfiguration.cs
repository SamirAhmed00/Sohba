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

            // Owner/Admin of the group
            builder.HasOne(g => g.Admin)
                   .WithMany(u => u.AdministeredGroups)
                   .HasForeignKey(g => g.AdminId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
