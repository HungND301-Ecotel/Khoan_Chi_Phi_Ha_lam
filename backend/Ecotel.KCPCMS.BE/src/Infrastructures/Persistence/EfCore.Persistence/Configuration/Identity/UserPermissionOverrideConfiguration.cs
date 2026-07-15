using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfCore.Persistence.Configuration.Identity;

public class UserPermissionOverrideConfiguration : IEntityTypeConfiguration<UserPermissionOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionOverride> builder)
    {
        builder.ToTable("UserPermissionOverrides", SchemaNames.Identity);

        builder.HasKey(x => x.Id);

        builder.HasQueryFilter(x => x.DeletedOn == null && x.User != null && x.User.DeletedOn == null);

        builder.Property(x => x.IsGranted)
               .IsRequired();

        builder.Property(x => x.Reason)
               .HasMaxLength(500)
               .IsRequired(false);

        builder.HasOne(x => x.User)
               .WithMany()
               .HasForeignKey(x => x.UserId)
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