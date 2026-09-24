using System;

namespace RealEstateInstallmentsManager.Services;

// Arabic day/month names for display, kept separate from .NET's culture APIs on
// purpose: Program.cs sets the app's global culture to en-US, so relying on
// CultureInfo("ar") formatting here would mean touching that unrelated global
// setting. A small local lookup avoids that (same reasoning already used for
// ReceiptViewInstallment's own month-name array).
public static class ArabicDateService
{
    private static readonly string[] Months =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    };

    private static readonly string[] Days =
    {
        "الأحد", "الاثنين", "الثلاثاء", "الأربعاء", "الخميس", "الجمعة", "السبت"
    };

    // e.g. "الخميس 24 سبتمبر 2026"
    public static string FormatFullDate(DateTime date) =>
        $"{Days[(int)date.DayOfWeek]} {date.Day} {Months[date.Month - 1]} {date.Year}";

    public static string Greeting(DateTime now) =>
        now.Hour < 12 ? "صباح الخير" : "مساء الخير";
}
