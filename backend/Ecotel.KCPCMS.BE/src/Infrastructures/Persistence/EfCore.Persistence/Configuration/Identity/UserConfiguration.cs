using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", SchemaNames.Identity);

        builder.Property(u => u.UserName).IsRequired().HasMaxLength(50);
        builder.Property(u => u.NormalizedUserName).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(500);
        builder.Property(u => u.PhoneNumber).HasMaxLength(15);
        builder.Property(u => u.Fullname).HasMaxLength(120);
        builder.Property(u => u.Avatar).HasMaxLength(256);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(256);
        builder.Property(u => u.RegisterProvider).HasMaxLength(50);
    }
}