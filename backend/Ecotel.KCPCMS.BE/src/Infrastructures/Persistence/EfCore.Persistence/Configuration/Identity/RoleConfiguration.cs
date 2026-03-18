using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", SchemaNames.Identity);

        builder.Property(r => r.Name).IsRequired().HasMaxLength(256);
        builder.Property(r => r.NormalizedName).IsRequired().HasMaxLength(256);
    }
}