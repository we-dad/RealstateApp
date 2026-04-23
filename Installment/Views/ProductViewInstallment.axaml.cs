using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;
using System.Collections.Generic;

namespace RealEstateApp.Views;

public partial class ProductViewInstallment : UserControl
{
    private readonly InstallmentDbService _db = new InstallmentDbService();
    private readonly OwnerInstallmentService _owners;
    private readonly ProductServiceInstallment _products;
    private ProductInstallment _product = new ProductInstallment();


    public ProductViewInstallment()
    {
        InitializeComponent();
        DataContext = _product;
        
        _db.Initialize();

        _owners = new OwnerInstallmentService(_db);
        _products = new ProductServiceInstallment(_db);

        LoadOwners();
        LoadProduct();
    }

    private void LoadOwners()
    {
        var owners = _owners.GetAll();

        OwnerBox.ItemsSource = owners;

        if (owners.Count > 0)
            OwnerBox.SelectedIndex = 0;
    }

    private void LoadProduct()
    {
        try
        {
            var data = _products.GetAll();
            ProductGrid.ItemsSource = null;
            ProductGrid.ItemsSource = data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (OwnerBox.SelectedItem is not OwnerInstallment owner)
                return;

            var ProductName = ProductNameBox.Text?.Trim() ?? "";
            var ProductMainPriceText = ProductMainPriceBox.Text?.Trim() ?? "1";
            var ProductType = _product.ProductType;
            
            if (!float.TryParse(ProductMainPriceText, out var ProductMainPrice) || ProductMainPrice < 1)
                ProductMainPrice = 1;
            else
                ProductMainPrice = float.Parse(ProductMainPriceText);
            
            var CarsPlateNumber = CarsPlateNumberBox.Text?.Trim() ?? "";
            var CarsVIN = CarsVINBox.Text?.Trim() ?? "";
            var CarsModel = CarsModelBox.Text?.Trim() ?? "";
            var CarsColor = CarsColorBox.Text?.Trim() ?? "";
            
            var MobileStorage = MobileStorageBox.Text?.Trim() ?? "";
            var MobileColor = MobileColorBox.Text?.Trim() ?? "";
        
                _products.Add(
                owner.Id,
                ProductName,
                ProductType,
                ProductMainPrice,
                CarsPlateNumber,
                CarsVIN,
                CarsModel,
                CarsColor,
                MobileStorage,
                MobileColor);
                
            // Clear form
            ProductNameBox.Text = "";
            ProductMainPriceBox.Text = "1";

            LoadProduct();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }


    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
        LoadProduct();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ProductInstallment product)
            new ProductWindowViewInstallment(product.Id).Show();
    }
}
