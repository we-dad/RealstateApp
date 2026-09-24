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

    // Guards against a real race: TryAutoLoginAsync (silent, on load) and
    // Login_Click (manual) both call _supabaseService.InitializeAsync(), which
    // replaces the shared Supabase Client with a new instance every time - two
    // sign-in attempts running at once could end up reading/replacing that
    // client from under each other (double navigation, or a mismatched
    // role/identity). The login button is disabled while either path is running,
    // so only one can ever be in flight.
    private bool _signInInProgress;

    public string AppVersion => $"Version {AppVersionService.GetVersion()}";

    public LoginView(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _supabaseService = supabaseService;
        DataContext = this;

        Loaded += (_, _) => StartConnectivityChecks();
        Loaded += async (_, _) => await TryAutoLoginAsync();
        Unloaded += (_, _) => _connectivityTimer?.Stop();
    }

    // Attempts a silent sign-in from a session saved by a previous "تذكرني"
    // login. Runs once when this screen loads, before the user touches anything;
    // if it fails (no saved session, expired, no internet), the login form just
    // sits there normally - never shows an error for this, since "nothing to
    // restore" is the everyday case, not a failure.
    private async Task TryAutoLoginAsync()
    {
        _signInInProgress = true;
        LoginButton.IsEnabled = false;
        try
        {
            await _supabaseService.InitializeAsync();
            if (!await _supabaseService.TryRestoreSessionAsync()) return;

            var auth = new AuthService(_supabaseService);
            await CompleteSignInAsync(auth);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            _signInInProgress = false;
            LoginButton.IsEnabled = true;
        }
    }

    private async Task CompleteSignInAsync(AuthService auth)
    {
        var roleService = new RoleService(_supabaseService);
        AppSession.Role = await roleService.GetMyRoleAsync();
        AppSession.UserId = auth.CurrentUserId ?? "";
        AppSession.DisplayName = auth.CurrentDisplayName;

        Console.WriteLine($"ROLE = {AppSession.Role}");

        _mainWindow.ShowMainMenu();
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
        if (_signInInProgress) return; // a silent auto-login attempt is still running
        _signInInProgress = true;
        LoginButton.IsEnabled = false;

        try
        {
            ErrorText.Text = "";

            var email = EmailBox.Text?.Trim() ?? "";
            var password = PasswordBox.Text?.Trim() ?? "";

            SessionPersistenceService.RememberMe = RememberMeCheckBox.IsChecked == true;

            await _supabaseService.InitializeAsync();

            var auth = new AuthService(_supabaseService);
            var ok = await auth.SignInAsync(email, password);

            if (!ok)
            {
                ErrorText.Text = "فشل تسجيل الدخول";
                return;
            }

            await CompleteSignInAsync(auth);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            ErrorText.Text = "حدث خطأ أثناء تسجيل الدخول";
        }
        finally
        {
            _signInInProgress = false;
            LoginButton.IsEnabled = true;
        }
    }
}
