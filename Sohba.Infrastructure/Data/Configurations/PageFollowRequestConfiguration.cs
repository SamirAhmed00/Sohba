using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PageFollowRequestConfiguration : IEntityTypeConfiguration<PageFollowRequest>
    {
        public void Configure(EntityTypeBuilder<PageFollowRequest> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(r => r.Message).IsRequired().HasMaxLength(500);

            builder.HasOne(r => r.Page)
                   .WithMany(p => p.FollowRequests)
                   .HasForeignKey(r => r.PageId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ReviewedByUser)
                   .WithMany()
                   .HasForeignKey(r => r.ReviewedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Filtered Index: exactly one pending request per user per page
            builder.HasIndex(r => new { r.PageId, r.UserId })
                   .HasFilter("[Status] = 1");

            builder.HasIndex(r => new { r.PageId, r.Status });
        }
    }
}
