using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.StoryAggregate;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class StoryReactionConfiguration : IEntityTypeConfiguration<StoryReaction>
    {
        public void Configure(EntityTypeBuilder<StoryReaction> builder)
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(r => r.Story)
                   .WithMany()
                   .HasForeignKey(r => r.StoryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // One reaction per (user, story) — prevents duplicate reactions.
            builder.HasIndex(r => new { r.StoryId, r.UserId }).IsUnique();
        }
    }
}