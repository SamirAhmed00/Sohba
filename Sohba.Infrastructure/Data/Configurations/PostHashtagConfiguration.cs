using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
    {
        public void Configure(EntityTypeBuilder<PostHashtag> builder)
        {
            // Composite Key for Many-to-Many join table
            builder.HasKey(ph => new { ph.PostId, ph.HashtagId });

            builder.HasOne(ph => ph.Post)
                   .WithMany(p => p.PostHashtags)
                   .HasForeignKey(ph => ph.PostId);

            builder.HasOne(ph => ph.Hashtag)
                   .WithMany(h => h.PostHashtags)
                   .HasForeignKey(ph => ph.HashtagId);
        }
    }
}
