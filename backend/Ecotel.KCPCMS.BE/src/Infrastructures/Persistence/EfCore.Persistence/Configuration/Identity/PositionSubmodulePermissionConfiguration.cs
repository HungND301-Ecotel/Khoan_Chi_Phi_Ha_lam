using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class PositionSubmodulePermissionConfiguration : IEntityTypeConfiguration<PositionSubmodulePermission>
{
    public void Configure(EntityTypeBuilder<PositionSubmodulePermission> builder)
    {
        builder.ToTable("PositionSubmodulePermissions", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedOn == null && x.Position != null && x.Position.DeletedOn == null);

        builder.Property(x => x.IsGranted)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasOne(x => x.Position)
               .WithMany()
               .HasForeignKey(x => x.PositionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SubModule)
               .WithMany()
               .HasForeignKey(x => x.SubModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
               .WithMany()
               .HasForeignKey(x => x.PermissionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}