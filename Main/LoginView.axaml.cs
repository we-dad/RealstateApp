using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class LoginView : UserControl
{
    private readonly MainWindow _mainWindow;
    private readonly SupabaseService _supabaseService;
    private DispatcherTimer? _connectivityTimer;
    public string AppVersion => $"Version {AppVersionService.GetVersion()}";

    public LoginView(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _supabaseService = supabaseService;
        DataContext = this;

        Loaded += (_, _) => StartConnectivityChecks();
        Unloaded += (_, _) => _connectivityTimer?.Stop();
    }

    private void TogglePassword_Click(object? sender, RoutedEventArgs e)
    {
        var hidden = PasswordBox.PasswordChar != default(char);
        PasswordBox.PasswordChar = hidden ? default : '*';
        ToolTip.SetTip(TogglePasswordButton, hidden ? "إخفاء كلمة المرور" : "إظهار كلمة المرور");
    }

    // Checks once immediately, then every 10 seconds while this screen is visible,
    // so someone sitting on the login screen sees the status update once their
    // internet comes back - a real check via ConnectivityService, not a static label.
    private void StartConnectivityChecks()
    {
        _connectivityTimer?.Stop();
        _ = CheckConnectivityAsync();
        _connectivityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _connectivityTimer.Tick += async (_, _) => await CheckConnectivityAsync();
        _connectivityTimer.Start();
    }

    private async Task CheckConnectivityAsync()
    {
        var online = await ConnectivityService.IsServerReachableAsync();
        ConnDot.Background = ThemeBrush(online ? "BrushOk" : "BrushLate");
        ConnStatusText.Text = online ? "متصل بالخادم" : "غير متصل بالإنترنت";
    }

    private static IBrush ThemeBrush(string key) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Gray;

    private async void Login_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            ErrorText.Text = "";

            var email = EmailBox.Text?.Trim() ?? "";
            var password = PasswordBox.Text?.Trim() ?? "";

            await _supabaseService.InitializeAsync();

            var auth = new AuthService(_supabaseService);
            var ok = await auth.SignInAsync(email, password);

            if (!ok)
            {
                ErrorText.Text = "فشل تسجيل الدخول";
                return;
            }

            var roleService = new RoleService(_supabaseService);
            AppSession.Role = await roleService.GetMyRoleAsync();
            AppSession.UserId = auth.CurrentUserId ?? "";

            Console.WriteLine($"ROLE = {AppSession.Role}");

            _mainWindow.ShowMainMenu();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            ErrorText.Text = "حدث خطأ أثناء تسجيل الدخول";
        }
    }
}
