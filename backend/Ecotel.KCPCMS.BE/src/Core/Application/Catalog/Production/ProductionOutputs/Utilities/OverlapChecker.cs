using Domain.Entities.Production;

namespace Application.Catalog.Production.ProductionOutputs.Utilities;

public static class OverlapChecker
{
    /// <summary>
    /// Kiểm tra hai khoảng thời gian có trùng lặp hay không
    /// Hiệu quả: O(1)
    /// </summary>
    public static bool IsOverlapping(DateOnly start1, DateOnly end1, DateOnly start2, DateOnly end2)
    {
        return start1 <= end2 && end1 >= start2;
    }

    /// <summary>
    /// Kiểm tra các khoảng thời gian trong danh sách có trùng lặp với nhau hay không
    /// Hiệu quả: O(n log n) - sắp xếp + kiểm tra liên tiếp
    /// </summary>
    public static bool HasOverlapInList(IEnumerable<(DateOnly Start, DateOnly End, int Index)> periods)
    {
        var sortedPeriods = periods.OrderBy(x => x.Start).ToList();

        for (int i = 0; i < sortedPeriods.Count - 1; i++)
        {
            if (sortedPeriods[i].End >= sortedPeriods[i + 1].Start)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra khoảng thời gian mới có trùng lặp với các bản ghi tồn tại hay không
    /// Hiệu quả: O(n) - sắp xếp n record + m request
    /// </summary>
    public static bool HasOverlapWithExisting(
        IEnumerable<(DateOnly Start, DateOnly End, int Index)> newPeriods,
        IEnumerable<ProductionOutput> existingRecords)
    {
        var existingList = existingRecords.ToList();

        if (!existingList.Any())
        {
            return HasOverlapInList(newPeriods);
        }

        var newPeriodsList = newPeriods.ToList();
        var allPeriods = newPeriodsList
            .Concat(existingList.Select((x, i) => (x.StartMonth, x.EndMonth, Index: newPeriodsList.Count + i)))
            .ToList();

        return HasOverlapInList(allPeriods);
    }

    /// <summary>
    /// Kiểm tra khoảng thời gian mới có trùng lặp với các bản ghi tồn tại (loại trừ exclude IDs)
    /// Sử dụng cho Update: không kiểm tra trùng với các record đang update
    /// Hiệu quả: O(n log n)
    /// </summary>
    public static bool HasOverlapWithExistingExclude(
        IEnumerable<(DateOnly Start, DateOnly End, int Index)> newPeriods,
        IEnumerable<ProductionOutput> existingRecords,
        IEnumerable<Guid> excludeIds)
    {
        var excludeSet = new HashSet<Guid>(excludeIds);
        var filteredExisting = existingRecords.Where(x => !excludeSet.Contains(x.Id)).ToList();

        return HasOverlapWithExisting(newPeriods, filteredExisting);
    }

    /// <summary>
    /// Lấy danh sách các cặp chỉ số có khoảng thời gian trùng lặp
    /// Hiệu quả: O(n log n)
    /// </summary>
    public static List<(int Index1, int Index2)> GetOverlappingPairs(
        IEnumerable<(DateOnly Start, DateOnly End, int Index)> periods)
    {
        var overlappingPairs = new List<(int, int)>();
        var sortedPeriods = periods.OrderBy(x => x.Start).ThenBy(x => x.End).ToList();

        for (int i = 0; i < sortedPeriods.Count - 1; i++)
        {
            for (int j = i + 1; j < sortedPeriods.Count; j++)
            {
                if (sortedPeriods[i].End < sortedPeriods[j].Start)
                {
                    break; // Không còn overlap nào với sortedPeriods[i] vì đã sắp xếp
                }

                if (IsOverlapping(sortedPeriods[i].Start, sortedPeriods[i].End,
                    sortedPeriods[j].Start, sortedPeriods[j].End))
                {
                    overlappingPairs.Add((sortedPeriods[i].Index, sortedPeriods[j].Index));
                }
            }
        }

        return overlappingPairs;
    }
}
