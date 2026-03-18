namespace Domain.Extensions;

public static class DateTimeExtensions
{
    public static DateTimeOffset ToCutOfDate(this DateTimeOffset dateTimeOffset)
    {
        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0,
            dateTimeOffset.Offset).AddDays(-1);
    }

    public static DateTimeOffset ToCutOffDateV2(this DateTimeOffset dateTimeOffset)
    {
        var newDate = dateTimeOffset.AddHours(7).Date.AddDays(-1);
        return new DateTimeOffset(newDate, dateTimeOffset.Offset);
    }

    public static DateTimeOffset ToUtcDate(this DateTimeOffset dateTimeOffset)
    {
        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0,
            TimeSpan.Zero);
    }

    public static DateTimeOffset StartOfCurrentMonth(this DateTimeOffset dateTimeOffset)
    {
        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, 1, 0, 0, 0,
            TimeSpan.Zero);
    }

    public static DateTimeOffset EndOfCurrentMonth(this DateTimeOffset dateTimeOffset)
    {
        int year = dateTimeOffset.Month == 12 ? dateTimeOffset.Year + 1 : dateTimeOffset.Year;
        int month = dateTimeOffset.Month == 12 ? 1 : dateTimeOffset.Month + 1;

        return new DateTimeOffset(year, month, 1, 0, 0, 0,
            TimeSpan.Zero).AddDays(-1);
    }

    public static DateTimeOffset StartOfCurrentWeek(this DateTimeOffset dateTimeOffset)
    {
        // start of current week but in monday
        int daysToSubtract = (int)dateTimeOffset.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysToSubtract < 0)
        {
            daysToSubtract += 7;
        }

        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0,
            TimeSpan.Zero).AddDays(-daysToSubtract);
    }

    public static DateTimeOffset EndOfCurrentWeek(this DateTimeOffset dateTimeOffset)
    {
        int daysToAdd = 7 - (int)dateTimeOffset.DayOfWeek;

        if (daysToAdd >= 7)
        {
            daysToAdd = 0;
        }

        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0,
            TimeSpan.Zero).AddDays(daysToAdd);
    }

    public static DateTimeOffset StartOfDay(this DateTimeOffset dateTimeOffset)
    {
        return new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, dateTimeOffset.Offset);
    }

    public static DateTimeOffset EndOfDay(this DateTimeOffset dateTimeOffset)
    {
        var nextDay = new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, dateTimeOffset.Offset).AddDays(1);

        return nextDay.AddSeconds(-1);
    }
}