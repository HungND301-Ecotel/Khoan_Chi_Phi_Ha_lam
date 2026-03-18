using TimeZoneConverter;

namespace Application.Helpers;

public static class DateTimeHelper
{
    public static long GenUnixTime()
    {
        var currentTime = DateTimeOffset.UtcNow;
        return currentTime.ToUnixTimeMilliseconds();
    }

    public static TimeSpan GetGmtOffsetByTimeZone(string timeZone)
    {
        var timeZoneId = TZConvert.GetTimeZoneInfo(timeZone);

        // Get the GMT offset
        var gmtOffset = timeZoneId.BaseUtcOffset;

        // Determine if the time zone is currently observing daylight saving time
        bool isDaylightSavingTime = timeZoneId.IsDaylightSavingTime(DateTime.UtcNow);

        // If observing daylight saving time, adjust the offset
        if (isDaylightSavingTime)
        {
            gmtOffset += new TimeSpan(1, 0, 0);
        }

        return gmtOffset;
    }
}