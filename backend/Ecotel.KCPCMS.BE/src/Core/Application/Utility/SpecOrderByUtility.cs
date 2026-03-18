namespace Application.Utility;

public static class SpecOrderByUtility
{
    public static string[]? CorrectNestedOrderBy(IList<string>? orderBy)
    {
        if (orderBy == null || !orderBy.Any())
        {
            orderBy = new List<string>
            {
                "CreatedOn desc"
            };
        }

        return orderBy.ToArray();
    }
}