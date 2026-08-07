using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(p => p.Content).IsRequired();

            // One User has many Posts
            builder.HasOne(p => p.User)
                   .WithMany(u => u.Posts)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // Avoid multiple cascade paths
            
            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasIndex(p => new { p.UserId, p.CreatedAt });
            builder.HasIndex(p => p.CreatedAt);
            builder.HasIndex(p => new { p.SourceType, p.SourceId });

        }
    }
}
