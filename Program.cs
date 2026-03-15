using System;
using Avalonia;
using RealEstateApp.Services;

namespace RealEstateApp;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var db = new DbService();
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
