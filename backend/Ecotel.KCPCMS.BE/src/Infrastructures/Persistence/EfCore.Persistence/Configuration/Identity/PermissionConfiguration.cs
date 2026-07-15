using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        // EF Core mặc định lưu Enum dưới dạng Int trong SQL.
        // Nếu muốn lưu dưới dạng chuỗi (Text) thì mở comment dòng dưới
        // builder.Property(x => x.Code).HasConversion<string>();
        builder.Property(x => x.Code)
               .IsRequired();

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(x => x.Description)
               .HasMaxLength(500)
               .IsRequired(false);

        // Code của Permission (VD: VIEW, CREATE, UPDATE, DELETE) là duy nhất
        builder.HasIndex(x => x.Code).IsUnique();
    }
}