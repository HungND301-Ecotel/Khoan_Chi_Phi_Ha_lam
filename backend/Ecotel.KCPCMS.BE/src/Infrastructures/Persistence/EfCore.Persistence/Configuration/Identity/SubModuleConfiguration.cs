using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class SubModuleConfiguration : IEntityTypeConfiguration<SubModule>
{
    public void Configure(EntityTypeBuilder<SubModule> builder)
    {
        builder.ToTable("SubModules", SchemaNames.Identity);

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

        // Foreign Key
        builder.HasOne(x => x.Module)
               .WithMany(x => x.SubModules)
               .HasForeignKey(x => x.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        // 1 SubModule code nên là duy nhất trong 1 Module
        builder.HasIndex(x => new { x.ModuleId, x.Code }).IsUnique();
    }
}