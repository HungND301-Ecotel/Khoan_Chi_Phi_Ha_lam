using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class UserClaimConfiguration : IEntityTypeConfiguration<UserClaim>
{
    public void Configure(EntityTypeBuilder<UserClaim> builder)
    {
        builder.ToTable("UserClaims", SchemaNames.Identity);

        builder.HasKey(uc => uc.Id);

        builder.Property(uc => uc.ClaimType).IsRequired();
        builder.Property(uc => uc.ClaimValue).IsRequired();

        builder.HasOne(uc => uc.User)
            .WithMany()
            .HasForeignKey(uc => uc.UserId)
            .IsRequired(false);
    }
}