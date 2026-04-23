using Avalonia.Controls;
using System;
using Avalonia.Diagnostics;
using Avalonia.Interactivity;
using Avalonia;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class CustomerWindowViewInstallment : Window
{
    private readonly CustomerInstallment _customer;
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly CustomerServiceInstallment _customerDB;


    public CustomerWindowViewInstallment(CustomerInstallment customer)
    {
        InitializeComponent();
        DataContext = new CustomerInstallment();

        _db.Initialize();
        _customerDB = new CustomerServiceInstallment(_db);

        _customer = customer;

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
            var job = JobBox.Text?.Trim() ?? "";
            
            var sponserName = SponserNameBox.Text?.Trim() ?? "";
            var sponserPhone = SponserPhoneBox.Text?.Trim() ?? "";
            var sponserIdentityNumber = SponserIdentityNumberBox.Text?.Trim() ?? "";
            var sponserAddress = SponserAddressBox.Text?.Trim() ?? "";
            var sponserJob = SponserJobBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _customerDB.Update(_customer.Id,name, identityNumber, phone, address,job,sponserName,sponserIdentityNumber,sponserPhone,sponserAddress,sponserJob);
            
            Refresh();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh()
    {
        var Refresh_Customer = _customerDB.GetById(_customer.Id);
        if (Refresh_Customer is null) return;

        NameBox.Text = Refresh_Customer.Name;
        PhoneBox.Text = Refresh_Customer.Phone;
        IdentityNumberBox.Text = Refresh_Customer.IdentityNumber;
        AddressBox.Text = Refresh_Customer.Address;
        JobBox.Text = Refresh_Customer.Job;
        
        BoolSponserBox.IsChecked = Refresh_Customer.BoolSponser;
        
        SponserNameBox.Text = Refresh_Customer.SponserName;
        SponserPhoneBox.Text = Refresh_Customer.SponserPhone;
        SponserIdentityNumberBox.Text = Refresh_Customer.SponserIdentityNumber;
        SponserAddressBox.Text = Refresh_Customer.SponserAddress;
        SponserJobBox.Text = Refresh_Customer.SponserJob;
        

        ResultNameBox.Text = Refresh_Customer.Name;
        ResultPhoneBox.Text = Refresh_Customer.Phone;
        ResultIdentityNumberBox.Text = Refresh_Customer.IdentityNumber;
        ResultAddressBox.Text = Refresh_Customer.Address;
        ResultJobBox.Text = Refresh_Customer.Job;
        
        ResultSponserNameBox.Text = Refresh_Customer.SponserName;
        ResultSponserPhoneBox.Text = Refresh_Customer.SponserPhone;
        ResultSponserIdentityNumberBox.Text = Refresh_Customer.SponserIdentityNumber;
        ResultSponserAddressBox.Text = Refresh_Customer.SponserAddress;
        ResultSponserJobBox.Text = Refresh_Customer.SponserJob;
    }
    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (_customer is null) return;
        try
        {
            _customerDB.Delete(_customer.Id);
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
