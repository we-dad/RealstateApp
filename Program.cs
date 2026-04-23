using System;
using System.Globalization;
using Avalonia;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var culture = new CultureInfo("en-US");
        culture.DateTimeFormat.Calendar = new GregorianCalendar();

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        
        try
        {
            var db = new DbServiceRealEstate();
            db.Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            Console.ReadLine();
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
