using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ProductWindowViewInstallment : Window
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly OwnerInstallmentService _owners;
    private readonly ProductServiceInstallment _products;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    private ProductInstallment? _product;
    private readonly long _productID;

    public ProductWindowViewInstallment(long productID, SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _owners = new OwnerInstallmentService(_db);
        _products = new ProductServiceInstallment(_db);
        
        OwnerGrid.DoubleTapped += OwnerGrid_DoubleTapped;

        _productID = productID;

        Refresh();
        
        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
    }

    private void LoadOwners()
    {
        var owners = _owners.GetAll();

        OwnerBox.ItemsSource = owners;
        OwnerBox.SelectedItem = owners.FirstOrDefault(o => o.Id == _product?.OwnerId);
    }

    private void Update_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_product is null)
                return;

            if (OwnerBox.SelectedItem is not OwnerInstallment owner)
                return;

            var productName = ProductNameBox.Text?.Trim() ?? "";
            var productMainPriceText = ProductMainPriceBox.Text?.Trim() ?? "1";
            var productType = _product.ProductType;

            if (!float.TryParse(productMainPriceText, out var productMainPrice) || productMainPrice < 1)
                productMainPrice = 1;

            var carsPlateNumber = CarsPlateNumberBox.Text?.Trim() ?? "";
            var carsVIN = CarsVINBox.Text?.Trim() ?? "";
            var carsModel = CarsModelBox.Text?.Trim() ?? "";
            var carsColor = CarsColorBox.Text?.Trim() ?? "";

            var mobileStorage = MobileStorageBox.Text?.Trim() ?? "";
            var mobileColor = MobileColorBox.Text?.Trim() ?? "";

            _products.Update(
                _productID,
                owner.Id,
                productName,
                productType,
                productMainPrice,
                carsPlateNumber,
                carsVIN,
                carsModel,
                carsColor,
                mobileStorage,
                mobileColor
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
        _product = _products.GetById(_productID);

        if (_product is null)
            return;

        DataContext = _product;

        LoadOwners();

        ProductNameBox.Text = _product.ProductName ?? "";
        ProductMainPriceBox.Text = _product.ProductMainPrice.ToString();

        CarsPlateNumberBox.Text = _product.CarPlateNumber ?? "";
        CarsVINBox.Text = _product.CarVIN ?? "";
        CarsModelBox.Text = _product.CarModel ?? "";
        CarsColorBox.Text = _product.CarColor ?? "";

        MobileStorageBox.Text = _product.MobileStorage ?? "";
        MobileColorBox.Text = _product.MobileColor ?? "";

        ResultProductNameBox.Text = _product.ProductName ?? "";
        ResultProductTypeBox.Text = _product.ProductType ?? "";
        ResultProductMainPriceBox.Text = _product.ProductMainPrice.ToString();
        ResultCarsPlateNumberBox.Text = _product.CarPlateNumber ?? "";
        ResultCarsVINBox.Text = _product.CarVIN ?? "";
        ResultCarsModelBox.Text = _product.CarModel ?? "";
        ResultCarsColorBox.Text = _product.CarColor ?? "";

        ResultMobileStorageBox.Text = _product.MobileStorage ?? "";
        ResultMobileColorBox.Text = _product.MobileColor ?? "";
        
        var gridOwner = _owners.GetById(_product.OwnerId);
        OwnerGrid.ItemsSource = gridOwner is null
            ? new List<OwnerInstallment>()
            : new List<OwnerInstallment> { gridOwner };
        
    }
    
    private async void OwnerGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (OwnerGrid.SelectedItem is not OwnerInstallment owner) return;

        var window = new OwnersWindowViewInstallment(owner, _supabaseService);
        await window.ShowDialog(this);

        Refresh();   // owner may have been edited — reload the row
    }
    
    private void Delete_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            _products.Delete(_productID);

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