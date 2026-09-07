using Avalonia.Controls;
using System;
using System.Threading.Tasks;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersWindowViewRealEstate : Window
{
    private readonly OwnerRealEstate _ownerRealEstate;
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _ownersDB;
    private readonly SupabaseService? _supabaseService;
    private readonly RealEstateSyncService? _sync;

    public OwnersWindowViewRealEstate(OwnerRealEstate ownerRealEstate ,SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _ownersDB = new OwnerServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

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

            _ = _sync.PushAllDirtyAsync();
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
        
        UnitsGrid.ItemsSource = _ownersDB.GetUnitsByOwnerId(_ownerRealEstate.Id);
    }
    
    private async void UnitsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (UnitsGrid.SelectedItem is not UnitRealEstate unit) return;

        var window = new UnitsWindowViewRealEstate(unit.Id, _supabaseService);
        await window.ShowDialog(this);
        Refresh();
    }
    
    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var freshOwner = _ownersDB.GetById(_ownerRealEstate.Id);
            if (freshOwner is null)
                return;

            _ownersDB.Delete(_ownerRealEstate.Id);

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
