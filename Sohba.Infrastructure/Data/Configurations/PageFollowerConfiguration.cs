using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PageFollowerConfiguration : IEntityTypeConfiguration<PageFollower>
    {
        public void Configure(EntityTypeBuilder<PageFollower> builder)
        {
            builder.HasKey(pf => new { pf.PageId, pf.UserId });

            builder.HasOne(pf => pf.Page)
                   .WithMany()
                   .HasForeignKey(pf => pf.PageId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pf => pf.User)
                   .WithMany(u => u.FollowedPages)
                   .HasForeignKey(pf => pf.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
