using Avalonia.Controls;
using System;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class TenantsWindowViewRealEstate : Window
{
    private readonly TenantRealEstate _tenantRealEstate;
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly TenantServiceRealEstate _tenantDB;


    public TenantsWindowViewRealEstate(TenantRealEstate tenantRealEstate)
    {
        InitializeComponent();

        _db.Initialize();
        _tenantDB = new TenantServiceRealEstate(_db);

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
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh()
    {
        var Refresh_tenant = _tenantDB.GetById(_tenantRealEstate.Id);
        if (Refresh_tenant is null) return;

        NameBox.Text = Refresh_tenant.Name;
        PhoneBox.Text = Refresh_tenant.Phone;
        IdentityNumberBox.Text = Refresh_tenant.IdentityNumber;
        AddressBox.Text = Refresh_tenant.Address;

        ResultNameBox.Text = Refresh_tenant.Name;
        ResultPhoneBox.Text = Refresh_tenant.Phone;
        ResultIdentityNumberBox.Text = Refresh_tenant.IdentityNumber;
        ResultAddressBox.Text = Refresh_tenant.Address;
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_tenantRealEstate is null) return;
        try
        {
            _tenantDB.Delete(_tenantRealEstate.Id);
            Close();
        }
        catch (InvalidOperationException ex)
        {
            await ShowMessageAsync("تنبيه", ex.Message);
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("خطأ", ex.Message);
        }
    }
    private async System.Threading.Tasks.Task ShowMessageAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var ok = new Button { Content = "موافق", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };

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
