using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;

namespace RealEstateApp.Views;

public partial class CustomerViewInstallment : UserControl
{
    private readonly InstallmentDbService _db = new InstallmentDbService();
    private readonly CustomerServiceInstallment _cutomerService;
    private CustomerInstallment _customer = new CustomerInstallment();

    public CustomerViewInstallment()
    {
        InitializeComponent();
        DataContext = _customer;

        _db.Initialize();
        _cutomerService = new CustomerServiceInstallment(_db);

        LoadCutomer();
    }

    private void LoadCutomer()
    {
        var data = _cutomerService.GetAll();
        
        // اجبار التحديث
        CustomerGrid.ItemsSource = null;
        CustomerGrid.ItemsSource = data;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
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

            _cutomerService.Add(name, identityNumber, phone, address,job,sponserName,sponserIdentityNumber,sponserPhone,sponserAddress,sponserJob);

            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";
            JobBox.Text = "";

            SponserNameBox.Text = "";
            SponserIdentityNumberBox.Text = "";
            SponserPhoneBox.Text = "";
            SponserAddressBox.Text = "";
            SponserJobBox.Text = "";

            LoadCutomer();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadCutomer();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CustomerInstallment customer)
            new CustomerWindowViewInstallment(customer).Show();
    }
}