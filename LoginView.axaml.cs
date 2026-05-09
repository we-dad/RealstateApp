using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class LoginView : UserControl
{
    private readonly MainWindow _mainWindow;
    private readonly SupabaseService _supabaseService;

    public LoginView(MainWindow mainWindow, SupabaseService supabaseService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _supabaseService = supabaseService;
    }

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