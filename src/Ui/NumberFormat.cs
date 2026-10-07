using System;
using System.Globalization;

namespace IdleBar.Ui;

public static class NumberFormat
{
    private static readonly NumberFormatInfo French = new() { NumberDecimalSeparator = ",", NumberGroupSeparator = " " };
    private static readonly string[] Suffixes = ["", " k", " M", " Md", " Bn", " Bd", " Tn"];

    public static string Amount(double value) =>
        value < 1000 ? Math.Floor(value).ToString("0", French) : WithSuffix(value);

    public static string Rate(double value) =>
        value < 1000 ? value.ToString("0.#", French) : WithSuffix(value);

    public static string Count(int count, string one, string many) => $"{Amount(count)} {(count > 1 ? many : one)}";

    public static string Coins(double value) => $"{Amount(value)} {(value < 2 ? "écu" : "écus")}";

    private static string WithSuffix(double value)
    {
        int tier = Math.Min((int)Math.Floor(Math.Log10(value) / 3), Suffixes.Length - 1);
        double scaled = value / Math.Pow(1000, tier);
        string format = scaled switch
        {
            < 10 => "0.00",
            < 100 => "0.0",
            _ => "0",
        };
        return scaled.ToString(format, French) + Suffixes[tier];
    }
}
