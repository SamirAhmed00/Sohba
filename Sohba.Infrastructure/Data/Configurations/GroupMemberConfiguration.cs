using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            // Composite Key to ensure user joins a group only once
            builder.HasKey(gm => new { gm.GroupId, gm.UserId });

            builder.HasOne(gm => gm.Group)
                   .WithMany(g => g.GroupMembers)
                   .HasForeignKey(gm => gm.GroupId);

            builder.HasOne(gm => gm.User)
                   .WithMany(u => u.GroupMemberships)
                   .HasForeignKey(gm => gm.UserId);
        }
    }
}
