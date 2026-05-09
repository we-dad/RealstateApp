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

        var laterButton = new Button
        {
            Content = "لاحقاً",
            Width = 120,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var updateButton = new Button
        {
            Content = "تحديث الآن",
            Width = 120,
            FontSize = 16,
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var messageText = new TextBlock
        {
            Text = "هل تريد تحميل وتثبيت التحديث الآن؟",
            FontSize = 18,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = Avalonia.Media.TextAlignment.Center
        };

        var confirmWindow = new Window
        {
            Width = 520,
            Height = 230,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Title = "تحديث",
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    messageText,
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
            try
            {
                updateButton.IsEnabled = false;
                laterButton.IsEnabled = false;

                messageText.Text = "...جاري تحميل التحديث";

                await manager.DownloadUpdatesAsync(update);

                messageText.Text = "جاري تثبيت التحديث...\nسيتم إغلاق التطبيق بعد قليل.";

                await Task.Delay(2000);

                manager.ApplyUpdatesAndExit(update);
            }
            catch (Exception ex)
            {
                messageText.Text = "فشل التحديث\n\n" + ex.Message;

                updateButton.IsEnabled = true;
                laterButton.IsEnabled = true;
            }
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