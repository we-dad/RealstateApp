using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersWindowViewInstallment : Window
{
    private readonly OwnerInstallment _owner;
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly OwnerInstallmentService _ownersDB;
    private readonly ProductServiceInstallment _productsDB; 
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    public OwnersWindowViewInstallment(OwnerInstallment owner, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _ownersDB = new OwnerInstallmentService(_db);
        _productsDB = new ProductServiceInstallment(_db);
        
        _owner = owner;
        
    _sync = new InstallmentSyncService(_db, _supabaseService);
    _ = _sync.PushAllDirtyAsync();
    
    ProductsGrid.DoubleTapped += ProductsGrid_DoubleTapped;

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

            _ownersDB.Update(_owner.Id, name, identityNumber, phone, address);

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
        var refreshedOwner = _ownersDB.GetById(_owner.Id);

        if (refreshedOwner is null)
            return;

        NameBox.Text = refreshedOwner.Name;
        PhoneBox.Text = refreshedOwner.Phone;
        IdentityNumberBox.Text = refreshedOwner.IdentityNumber;
        AddressBox.Text = refreshedOwner.Address;

        ResultNameBox.Text = refreshedOwner.Name;
        ResultPhoneBox.Text = refreshedOwner.Phone;
        ResultIdentityNumberBox.Text = refreshedOwner.IdentityNumber;
        ResultAddressBox.Text = refreshedOwner.Address;
        
        ProductsGrid.ItemsSource = _ownersDB.GetProductsByOwnerId(_owner.Id);
    }
    
    private void ProductsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductInstallment selected)
            return;

        var window = new ProductWindowViewInstallment(selected.Id, _supabaseService);
        window.Show();
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _ownersDB.Delete(_owner.Id);
            
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
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center
                },
                ok
            }
        };

        await dialog.ShowDialog(this);
    }
}