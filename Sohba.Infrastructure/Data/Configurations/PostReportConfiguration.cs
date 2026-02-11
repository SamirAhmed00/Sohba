using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.PostAggregate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class PostReportConfiguration : IEntityTypeConfiguration<PostReport>
    {
        public void Configure(EntityTypeBuilder<PostReport> builder)
        {
            builder.HasKey(pr => pr.Id);
            builder.Property(pr => pr.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.HasOne(pr => pr.Post)
                   .WithMany(p => p.Reports)
                   .HasForeignKey(pr => pr.PostId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(pr => pr.User) 
                   .WithMany(u => u.SentReports)
                   .HasForeignKey(pr => pr.UserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
