using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class UserSignatureConfiguration : IEntityTypeConfiguration<UserSignature>
{
    public void Configure(EntityTypeBuilder<UserSignature> builder)
    {
        builder.ToTable("UserSignatures", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SignatureFile).HasMaxLength(500);
        builder.Property(x => x.CertificateId).HasMaxLength(256);
        builder.Property(x => x.CertificateFile).HasMaxLength(500);
        builder.Property(x => x.PinHash).HasMaxLength(500);

        builder.HasOne(x => x.User)
            .WithMany(u=>u.UserSignatures)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}