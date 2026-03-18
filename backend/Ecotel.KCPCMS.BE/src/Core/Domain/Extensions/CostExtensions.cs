using Domain.Entities.Index;

namespace Domain.Extensions
{
    public static class CostExtensions
    {
        public static bool HasOverlap(this IEnumerable<Cost> costs)
        {
            if (costs == null)
            {
                return false;
            }

            var sorted = costs
                .OrderBy(c => c.StartMonth)
                .ThenBy(c => c.EndMonth)
                .ToList();

            for (int i = 1; i < sorted.Count; i++)
            {
                var prev = sorted[i - 1];
                var curr = sorted[i];

                if (prev.EndMonth >= curr.StartMonth)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasOverlapWith(
            this IEnumerable<Cost> costs,
            DateOnly newStart,
            DateOnly newEnd,
            Cost? costToExclude = null)
        {
            return costs
                .Where(c => costToExclude == null || c.Id != costToExclude.Id)
                .Any(c => c.StartMonth < newEnd && c.EndMonth > newStart);
        }
    }

}
