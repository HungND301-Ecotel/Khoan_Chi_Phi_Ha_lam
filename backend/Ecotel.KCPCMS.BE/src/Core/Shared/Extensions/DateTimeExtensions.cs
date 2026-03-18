namespace Shared.Extensions;

public static class DateTimeExtensions
{
    public static DateTime GetEndOfDate(this DateTimeOffset date) => date.Date.AddDays(1).AddTicks(-1);
}