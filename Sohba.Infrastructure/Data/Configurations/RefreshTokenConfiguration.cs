using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sohba.Domain.Entities.UserAggregate;
using System;

namespace Sohba.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(rt => rt.Id);
            builder.Property(rt => rt.Id).HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(rt => rt.TokenHash)
                   .IsRequired()
                   .HasMaxLength(64);
            builder.HasIndex(rt => rt.TokenHash).IsUnique();

            builder.Property(rt => rt.UserId).IsRequired();
            builder.HasIndex(rt => rt.UserId);

            builder.HasIndex(rt => rt.ExpiresAtUtc); // expiry-based cleanup

            builder.Property(rt => rt.CreatedByIp).HasMaxLength(64);
            builder.Property(rt => rt.RevokedByIp).HasMaxLength(64);
            builder.Property(rt => rt.ReplacedByTokenHash).HasMaxLength(64);

            builder.HasOne(rt => rt.User)
                   .WithMany()
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}