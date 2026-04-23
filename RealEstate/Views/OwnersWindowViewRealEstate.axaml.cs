using Avalonia.Controls;
using System;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersWindowViewRealEstate : Window
{
    private readonly OwnerRealEstate _ownerRealEstate;
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _ownersDB;


    public OwnersWindowViewRealEstate(OwnerRealEstate ownerRealEstate)
    {
        InitializeComponent();

        _db.Initialize();
        _ownersDB = new OwnerServiceRealEstate(_db);

        _ownerRealEstate = ownerRealEstate;

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

            _ownersDB.Update(_ownerRealEstate.Id, name, identityNumber, phone, address);

            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh()
    {
        var Refresh_owner = _ownersDB.GetById(_ownerRealEstate.Id);
        if (Refresh_owner is null) return;

        NameBox.Text = Refresh_owner.Name;
        PhoneBox.Text = Refresh_owner.Phone;
        IdentityNumberBox.Text = Refresh_owner.IdentityNumber;
        AddressBox.Text = Refresh_owner.Address;

        ResultNameBox.Text = Refresh_owner.Name;
        ResultPhoneBox.Text = Refresh_owner.Phone;
        ResultIdentityNumberBox.Text = Refresh_owner.IdentityNumber;
        ResultAddressBox.Text = Refresh_owner.Address;
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ownersDB.Delete(_ownerRealEstate.Id);
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
