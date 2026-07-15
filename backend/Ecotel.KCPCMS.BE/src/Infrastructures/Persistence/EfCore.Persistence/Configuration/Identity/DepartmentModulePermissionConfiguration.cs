using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class DepartmentModulePermissionConfiguration : IEntityTypeConfiguration<DepartmentModulePermission>
{
    public void Configure(EntityTypeBuilder<DepartmentModulePermission> builder)
    {
        builder.ToTable("DepartmentModulePermissions", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedOn == null && x.Department != null && x.Department.DeletedOn == null);

        builder.Property(x => x.IsGranted)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasOne(x => x.Department)
               .WithMany()
               .HasForeignKey(x => x.DepartmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Module)
               .WithMany()
               .HasForeignKey(x => x.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Permission)
               .WithMany()
               .HasForeignKey(x => x.PermissionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}