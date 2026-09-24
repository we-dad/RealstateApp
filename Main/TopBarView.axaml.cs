using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

// Shared top bar: brand (click = back to the module picker), a real online/offline
// indicator, the signed-in user's identity, and sign-out. Meant to be embedded at
// the top of every screen going forward, starting with MainMenuView.
public partial class TopBarView : UserControl
{
    private MainWindow? _mainWindow;
    private SupabaseService? _supabaseService;
    private DispatcherTimer? _connectivityTimer;

    // Parameterless constructor so XAML (<views:TopBarView x:Name="TopBar"/> in a
    // parent view) can instantiate it directly - real wiring happens in Init(),
    // called from the parent's own code-behind once it has both dependencies.
    // Same pattern already used by DashboardViewInstallment in this codebase.
    public TopBarView()
    {
        InitializeComponent();
        Loaded += (_, _) => StartConnectivityChecks();
        Unloaded += (_, _) => _connectivityTimer?.Stop();
    }

    public void Init(MainWindow mainWindow, SupabaseService supabaseService)
    {
        _mainWindow = mainWindow;
        _supabaseService = supabaseService;

        var name = string.IsNullOrWhiteSpace(AppSession.DisplayName) ? "مستخدم" : AppSession.DisplayName;
        UserNameText.Text = name;
        UserRoleText.Text = RoleDisplayName(AppSession.Role);
        AvatarText.Text = name.Length > 0 ? name[..1].ToString() : "؟";
    }

    private static string RoleDisplayName(string role) => role switch
    {
        "admin" => "مدير",
        "editor" => "محرر",
        "viewer" => "مشاهد",
        _ => "تجريبي"
    };

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
        ConnText.Text = online ? "متصل بالخادم" : "غير متصل بالإنترنت";
    }

    private static IBrush ThemeBrush(string key) =>
        Application.Current?.TryFindResource(key, out var value) == true && value is IBrush brush
            ? brush
            : Brushes.Gray;

    private void Brand_Click(object? sender, RoutedEventArgs e)
    {
        _mainWindow?.ShowMainMenu();
    }

    private async void Logout_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_supabaseService != null)
            {
                var auth = new AuthService(_supabaseService);
                await auth.SignOutAsync();
            }
        }
        catch (Exception ex)
        {
            // Sign-out failing server-side shouldn't trap the user on a signed-out-
            // looking screen with no way back in - clear local state and send them
            // to the login screen regardless.
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            // Belt-and-suspenders: SignOutAsync should already trigger the SDK's
            // own DestroySession via the registered persistence, but an explicit
            // delete here means a "تذكرني" session can never survive an actual
            // logout even if that assumption is ever wrong.
            new SessionPersistenceService().DestroySession();
            AppSession.Clear();
            _mainWindow?.ShowLogin();
        }
    }
}
