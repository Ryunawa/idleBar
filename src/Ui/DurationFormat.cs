using System;
using System.Globalization;

namespace IdleBar.Ui;

public static class DurationFormat
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public static string Span(TimeSpan duration)
    {
        int minutes = (int)Math.Ceiling(duration.TotalMinutes);
        if (minutes < 1)
        {
            return "quelques secondes";
        }

        if (minutes < 60)
        {
            return $"{minutes.ToString(French)} min";
        }

        int hours = minutes / 60;
        int rest = minutes % 60;
        return rest == 0 ? $"{hours.ToString(French)} h" : $"{hours.ToString(French)} h {rest.ToString("00", French)}";
    }

    public static string ClockTime(DateTimeOffset moment) => moment.ToLocalTime().ToString("HH:mm", French);
}
