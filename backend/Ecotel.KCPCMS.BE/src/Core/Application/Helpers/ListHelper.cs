namespace Application.Helpers;

public static class ListHelper
{
    public static List<T> Difference<T>(IEnumerable<T> left, IEnumerable<T> right)
    {
        var leftQueue = new Queue<T>();
        var leftSet = new HashSet<T>();
        var rightSet = new HashSet<T>();

        foreach (var item in left)
        {
            if (leftSet.Contains(item))
            {
                continue;
            }

            leftQueue.Enqueue(item);
            leftSet.Add(item);
        }

        foreach (var item in right)
        {
            rightSet.Add(item);
        }

        return leftQueue.Where(leftItem => !rightSet.Contains(leftItem)).ToList();
    }

    public static List<TLeft> DifferenceWith<TLeft, TRight, T>(
        IEnumerable<TLeft> left,
        IEnumerable<TRight> right,
        Func<TLeft, T> leftSelector,
        Func<TRight, T> rightSelector)
    {
        var leftQueue = new Queue<TLeft>();
        var leftSet = new HashSet<T>();
        var rightSet = new HashSet<T>();

        foreach (var item in left)
        {
            var leftSelection = leftSelector(item);
            if (leftSet.Contains(leftSelection))
            {
                continue;
            }

            leftQueue.Enqueue(item);
            leftSet.Add(leftSelection);
        }

        foreach (var item in right.Select(rightSelector))
        {
            rightSet.Add(item);
        }

        return leftQueue.Where(leftItem => !rightSet.Contains(leftSelector(leftItem))).ToList();
    }
}