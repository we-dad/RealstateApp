using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class TenantsWindowViewRealEstate : Window
{
    private readonly TenantRealEstate _tenantRealEstate;
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly TenantServiceRealEstate _tenantDB;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public TenantsWindowViewRealEstate(TenantRealEstate tenantRealEstate, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _tenantDB = new TenantServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

        _tenantRealEstate = tenantRealEstate;

        Refresh();
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _tenantDB.Update(_tenantRealEstate.Id, name, identityNumber, phone, address);

            Refresh();

            _ = _sync.PushAllDirtyAsync();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
    
    private void Refresh()
    {
        var refreshTenant = _tenantDB.GetById(_tenantRealEstate.Id);
        if (refreshTenant is null) return;

        NameBox.Text = refreshTenant.Name;
        PhoneBox.Text = refreshTenant.Phone;
        IdentityNumberBox.Text = refreshTenant.IdentityNumber;
        AddressBox.Text = refreshTenant.Address;

        ResultNameBox.Text = refreshTenant.Name;
        ResultPhoneBox.Text = refreshTenant.Phone;
        ResultIdentityNumberBox.Text = refreshTenant.IdentityNumber;
        ResultAddressBox.Text = refreshTenant.Address;
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_tenantRealEstate is null) return;

        try
        {
            _tenantDB.Delete(_tenantRealEstate.Id);

            _ = _sync.PushAllDirtyAsync();
            
            Close();
        }
        catch (InvalidOperationException ex)
        {
            _ = ShowMessageAsync("تنبيه", ex.Message);
        }
        catch (Exception ex)
        {
            _ = ShowMessageAsync("خطأ", ex.Message);
        }
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var ok = new Button
        {
            Content = "موافق",
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        ok.Click += (_, __) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    TextAlignment = Avalonia.Media.TextAlignment.Center
                },
                ok
            }
        };

        await dialog.ShowDialog(this);
    }
}