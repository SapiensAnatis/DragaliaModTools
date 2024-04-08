using System.Globalization;

namespace ModTools.Shared;

internal static class DateTimeHelper
{
    private const string FormatString = "yyyy/MM/dd HH:mm:ss";

    public static DateTimeOffset ParseDate(string date)
    {
        DateTime dateTime = DateTime.ParseExact(
            date,
            FormatString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal
        );

        return new DateTimeOffset(dateTime, TimeSpan.Zero);
    }

    public static string FormatDate(DateTimeOffset date)
    {
        return date.UtcDateTime.ToString(FormatString, CultureInfo.InvariantCulture);
    }
}
