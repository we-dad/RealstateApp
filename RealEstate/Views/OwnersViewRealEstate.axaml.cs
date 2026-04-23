using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Data.Common;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _owners;

    public OwnersViewRealEstate()
    {
        InitializeComponent();

        _db.Initialize();
        _owners = new OwnerServiceRealEstate(_db);

        LoadOwners();
    }

    private void LoadOwners()
    {
        var data = _owners.GetAll();

        Console.WriteLine($"Owners count: {data.Count}");

        // اجبار التحديث
        OwnersGrid.ItemsSource = null;
        OwnersGrid.ItemsSource = data;
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

            _owners.Add(name, IdentityNumber, phone, address);

            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";

            LoadOwners();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is OwnerRealEstate owner)
            new OwnersWindowViewRealEstate(owner).Show();
    }
}
