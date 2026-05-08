using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using RealEstateInstallmentsManager.Services;
using RealEstateInstallmentsManager.Views;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace RealEstateInstallmentsManager;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var culture = new CultureInfo("en-US");
        culture.DateTimeFormat.Calendar = new GregorianCalendar();

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        try
        {
            CheckForUpdatesWithUI();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Update check failed:");
            Console.WriteLine(ex);
        }

        try
        {
            var db = new DbServiceRealEstate();
            db.Initialize();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
  public static async Task CheckForUpdatesWithUI()
{
    try
    {
        var source = new GithubSource(
            "https://github.com/we-dad/real-estate-installment-manager-releases",
            null,
            false
        );

        var manager = new UpdateManager(source);

        var update = await manager.CheckForUpdatesAsync();

        if (update == null)
            return;

        var mainWindow =
            (Application.Current?.ApplicationLifetime
                as IClassicDesktopStyleApplicationLifetime)?
            .MainWindow;

        if (mainWindow == null)
            return;

        var updateButton = new Button
        {
            Content = "تحديث الآن",
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var laterButton = new Button
        {
            Content = "لاحقًا",
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var confirmWindow = new Window
        {
            Width = 420,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = "تحديث",
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15,
                Children =
                {
                    new TextBlock
                    {
                        Text = "تم التحقق من وجود تحدث هل تريد التحديث الآن؟",
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Spacing = 10,
                        Children =
                        {
                            laterButton,
                            updateButton
                            
                        }
                    }
                }
            }
        };

        updateButton.Click += async (_, _) =>
        {
            updateButton.IsEnabled = false;
            laterButton.IsEnabled = false;

            var contentPanel = (StackPanel)confirmWindow.Content!;

            ((TextBlock)contentPanel.Children[0]).Text = "جاري تحديث التطبيق";
            ((TextBlock)contentPanel.Children[1]).Text = "جاري تحميل التحديث...";

            await manager.DownloadUpdatesAsync(update);

            ((TextBlock)contentPanel.Children[1]).Text = "جاري تثبيت التحديث...";

            await Task.Delay(1500);

            manager.ApplyUpdatesAndRestart(update);
        };

        laterButton.Click += (_, _) =>
        {
            confirmWindow.Close();
        };

        await confirmWindow.ShowDialog(mainWindow);
    }
    catch (NotInstalledException)
    {
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}