using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;

namespace RealEstateApp.Views;

public partial class TenantsView : UserControl
{
    private readonly DbService _db = new DbService();
    private readonly TenantService _tenants;

    public TenantsView()
    {
        InitializeComponent();

        _db.Initialize();
        _tenants = new TenantService(_db);

        LoadTenants();
    }

    private void LoadTenants()
    {
        var data = _tenants.GetAll();

        Console.WriteLine($"Owners count: {data.Count}");

        // اجبار التحديث
        TenantsGrid.ItemsSource = null;
        TenantsGrid.ItemsSource = data;
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var IdentityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _tenants.Add(name, IdentityNumber, phone, address);

            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";

            LoadTenants();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadTenants();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Tenant tenant)
            new TenantsWindowView(tenant).Show();
    }
}
