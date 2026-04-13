using Domain.Common.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EfCore.Persistence.Extensions;

public static class SoftDeleteCascadeExtension
{
    public static void ApplySoftDeleteCascade(
    this ChangeTracker changeTracker,
    ISoftDelete entity,
    long deletedBy,
    DateTimeOffset deletedOn,
    HashSet<object>? visited = null)
    {
        visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance);

        if (!visited.Add(entity))
        {
            return;
        }

        var entry = changeTracker.Context.Entry(entity);

        // Lấy tất cả FK relationships mà entity này là Principal (cha)
        var entityType = entry.Metadata;
        var cascadeNavigations = entityType
            .GetReferencingForeignKeys()
            .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(fk => fk.PrincipalToDependent?.Name)
            .Where(name => name is not null)
            .ToHashSet();

        foreach (var navigation in entry.Navigations)
        {
            // ✅ Chỉ xử lý navigation nào được config Cascade
            if (!cascadeNavigations.Contains(navigation.Metadata.Name))
            {
                continue;
            }

            if (!navigation.IsLoaded)
            {
                try { navigation.Load(); }
                catch { continue; }
            }

            IEnumerable<object> relatedEntities = navigation switch
            {
                CollectionEntry col => col.CurrentValue?.Cast<object>()
                                       ?? Enumerable.Empty<object>(),
                ReferenceEntry refNav => refNav.CurrentValue is not null
                                       ? new[] { refNav.CurrentValue }
                                       : Enumerable.Empty<object>(),
                _ => Enumerable.Empty<object>()
            };

            foreach (var related in relatedEntities)
            {
                if (related is not ISoftDelete softDeletable)
                {
                    continue;
                }

                if (softDeletable.DeletedOn is not null)
                {
                    continue;
                }

                softDeletable.DeletedOn = deletedOn;
                softDeletable.DeletedBy = deletedBy;

                changeTracker.Context.Entry(related).State = EntityState.Modified;

                // Đệ quy xuống tầng tiếp theo
                changeTracker.ApplySoftDeleteCascade(softDeletable, deletedBy, deletedOn, visited);
            }
        }
    }
}
