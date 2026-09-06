using System;

namespace VenodorManagementFrontend.Helpers;

public static class IstTime
{
    private static readonly TimeZoneInfo India = ResolveIndiaTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, India);

    public static DateTime ToIst(this DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(value, India);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return TimeZoneInfo.ConvertTime(value, India);
        }

        // Unspecified values were usually saved with DateTime.Now (already IST wall clock).
        return value;
    }

    public static DateTime? ToIst(this DateTime? value) => value?.ToIst();

    public static string ToIst(this DateTime value, string format) => value.ToIst().ToString(format);

    public static string ToIst(this DateTime? value, string format) =>
        value.HasValue ? value.Value.ToIst(format) : string.Empty;

    private static TimeZoneInfo ResolveIndiaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }
}
