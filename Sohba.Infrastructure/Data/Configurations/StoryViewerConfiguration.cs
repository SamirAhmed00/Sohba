using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities;
using Sohba.Domain.Entities.StoryAggregate;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class StoryViewerConfiguration : IEntityTypeConfiguration<StoryViewer>
    {
        public void Configure(EntityTypeBuilder<StoryViewer> builder)
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(v => v.Story)
                   .WithMany(s => s.Viewers)
                   .HasForeignKey(v => v.StoryId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.User)
                   .WithMany()
                   .HasForeignKey(v => v.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}