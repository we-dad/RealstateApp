using Avalonia.Controls;
using Avalonia.Interactivity;
using RealEstateApp.Models;
using RealEstateApp.Services;
using System;
using System.Collections.Generic;

namespace RealEstateApp.Views;

public partial class ProductWindowViewInstallment : Window
{

    private readonly InstallmentDbService _db = new InstallmentDbService();
    private readonly OwnerInstallmentService _owners;
    private readonly ProductServiceInstallment _products;
    private ProductInstallment? _product;
    private long _productID;


    public ProductWindowViewInstallment(long productID)
    {
        InitializeComponent();

        _db.Initialize();
        _owners = new OwnerInstallmentService(_db);
        _products = new ProductServiceInstallment(_db);

        _productID = productID;

        Refresh();
    }

    private void LoadOwners()
    {
        var owners = _owners.GetAll();

        OwnerBox.ItemsSource = owners;

        if (owners.Count > 0)
            OwnerBox.SelectedIndex = 0;
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
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
        
            _products.Update(
                _productID,
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
                
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }


    private void Refresh()
    {
        _product = _products.GetById(_productID) ?? new ProductInstallment();
        if (_product is null) return;

        DataContext = _product;

        LoadOwners();

        OwnerBox.SelectedItem = _product.OwnerId;
        ProductNameBox.Text = _product.ProductName ?? "";
        ProductMainPriceBox.Text = _product.ProductMainPrice.ToString();

        //product
        ResultProductNameBox.Text = _product.ProductName ?? "";
        ResultProductTypeBox.Text = _product.ProductType ?? "";
        ResultProductMainPriceBox.Text = _product.ProductMainPrice.ToString();
        ResultCarsPlateNumberBox.Text = _product.CarPlateNumber ?? "";
            ResultCarsVINBox.Text = _product.CarVIN ?? "";
        ResultCarsModelBox.Text = _product.CarModel ?? "";
            ResultCarsColorBox.Text = _product.CarColor ?? "";
                
        ResultMobileStorageBox.Text = _product.MobileStorage ?? "";
            ResultMobileColorBox.Text = _product.MobileColor ?? "";

        //owner
        ResultNameBox.Text = _product.OwnerName ?? "";
        ResultIdentityNumberBox.Text = _product.OwnerIdentityNumber ?? "";
        ResultPhoneBox.Text = _product.OwnerPhone ?? "";
        ResultAddressBox.Text = _product.OwnerAddress ?? "";

    }
}
