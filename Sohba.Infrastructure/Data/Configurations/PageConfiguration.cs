using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.GroupAndPage;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PageConfiguration : IEntityTypeConfiguration<Page>
    {
        public void Configure(EntityTypeBuilder<Page> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(p => p.Admin)
                   .WithMany(u => u.AdministeredPages)
                   .HasForeignKey(p => p.AdminId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(p => p.Rules).HasMaxLength(2000);
            builder.Property(p => p.IsPrivate).HasDefaultValue(false);

            builder.HasMany(p => p.Followers)
                   .WithOne(pf => pf.Page)
                   .HasForeignKey(pf => pf.PageId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.FollowRequests)
                   .WithOne(r => r.Page)
                   .HasForeignKey(r => r.PageId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
