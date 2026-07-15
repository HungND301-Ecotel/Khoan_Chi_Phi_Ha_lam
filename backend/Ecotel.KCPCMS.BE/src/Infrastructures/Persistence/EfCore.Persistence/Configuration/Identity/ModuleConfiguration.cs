using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class ModuleConfiguration : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> builder)
    {
        builder.ToTable("Modules", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(x => x.Code)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.Description)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.Property(x => x.SortOrder)
               .IsRequired()
               .HasDefaultValue(0);

        // Đánh index cho Code để truy vấn nhanh nếu cần
        builder.HasIndex(x => x.Code).IsUnique();
    }
}