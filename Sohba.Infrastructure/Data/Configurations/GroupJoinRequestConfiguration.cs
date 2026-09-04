using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class GroupJoinRequestConfiguration : IEntityTypeConfiguration<GroupJoinRequest>
    {
        public void Configure(EntityTypeBuilder<GroupJoinRequest> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(r => r.Reason).HasMaxLength(500);

            builder.HasOne(r => r.Group)
                   .WithMany(g => g.JoinRequests)
                   .HasForeignKey(r => r.GroupId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ReviewedByUser)
                   .WithMany()
                   .HasForeignKey(r => r.ReviewedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Filtered Index: exactly one pending request per user per group
            builder.HasIndex(r => new { r.GroupId, r.UserId })
                   .HasFilter("[Status] = 1"); // Pending = 1

            builder.HasIndex(r => new { r.GroupId, r.Status });
        }
    }
}
