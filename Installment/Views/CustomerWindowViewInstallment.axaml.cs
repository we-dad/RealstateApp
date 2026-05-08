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

public partial class CustomerWindowViewInstallment : Window
{
    private readonly CustomerInstallment _customer;
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly CustomerServiceInstallment _customerDB;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;
    
    public CustomerWindowViewInstallment(CustomerInstallment customer, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        DataContext = new CustomerInstallment();

        _db.Initialize();
        _customerDB = new CustomerServiceInstallment(_db);

        _customer = customer;

        Refresh();
        
        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
    }
    

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";
            var job = JobBox.Text?.Trim() ?? "";

            var sponserName = SponserNameBox.Text?.Trim() ?? "";
            var sponserPhone = SponserPhoneBox.Text?.Trim() ?? "";
            var sponserIdentityNumber = SponserIdentityNumberBox.Text?.Trim() ?? "";
            var sponserAddress = SponserAddressBox.Text?.Trim() ?? "";
            var sponserJob = SponserJobBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _customerDB.Update(
                _customer.Id,
                name,
                identityNumber,
                phone,
                address,
                job,
                sponserName,
                sponserIdentityNumber,
                sponserPhone,
                sponserAddress,
                sponserJob
            );

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
        var refreshedCustomer = _customerDB.GetById(_customer.Id);

        if (refreshedCustomer is null)
            return;

        NameBox.Text = refreshedCustomer.Name;
        PhoneBox.Text = refreshedCustomer.Phone;
        IdentityNumberBox.Text = refreshedCustomer.IdentityNumber;
        AddressBox.Text = refreshedCustomer.Address;
        JobBox.Text = refreshedCustomer.Job;

        BoolSponserBox.IsChecked = refreshedCustomer.BoolSponser;

        SponserNameBox.Text = refreshedCustomer.SponserName;
        SponserPhoneBox.Text = refreshedCustomer.SponserPhone;
        SponserIdentityNumberBox.Text = refreshedCustomer.SponserIdentityNumber;
        SponserAddressBox.Text = refreshedCustomer.SponserAddress;
        SponserJobBox.Text = refreshedCustomer.SponserJob;

        ResultNameBox.Text = refreshedCustomer.Name;
        ResultPhoneBox.Text = refreshedCustomer.Phone;
        ResultIdentityNumberBox.Text = refreshedCustomer.IdentityNumber;
        ResultAddressBox.Text = refreshedCustomer.Address;
        ResultJobBox.Text = refreshedCustomer.Job;

        ResultSponserNameBox.Text = refreshedCustomer.SponserName;
        ResultSponserPhoneBox.Text = refreshedCustomer.SponserPhone;
        ResultSponserIdentityNumberBox.Text = refreshedCustomer.SponserIdentityNumber;
        ResultSponserAddressBox.Text = refreshedCustomer.SponserAddress;
        ResultSponserJobBox.Text = refreshedCustomer.SponserJob;
    }

    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_customer is null)
            return;

        try
        {
            _customerDB.Delete(_customer.Id);

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