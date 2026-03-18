using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class UserVerificationConfiguration : IEntityTypeConfiguration<UserVerification>
{
    public void Configure(EntityTypeBuilder<UserVerification> builder)
    {
        builder.ToTable("UserVerifications", SchemaNames.Identity);

        // Add index for better performance on frequently queried fields
        builder.HasIndex(uv => uv.Email);
        builder.HasIndex(uv => uv.Phone);
        builder.HasIndex(uv => uv.Token);
        builder.HasIndex(uv => uv.VerificationCode);
    }
}