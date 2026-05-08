using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class ProductViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly OwnerInstallmentService _owners;
    private readonly ProductServiceInstallment _products;
    private ProductInstallment _product = new ProductInstallment();
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;

    public ProductViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        DataContext = _product;

        _db.Initialize();

        _owners = new OwnerInstallmentService(_db);
        _products = new ProductServiceInstallment(_db);

        LoadOwners();
        LoadProduct();

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
        _ = SyncProductsFromCloudAsync();
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

            _products.Add(
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

            ProductNameBox.Text = "";
            ProductMainPriceBox.Text = "1";
            CarsPlateNumberBox.Text = "";
            CarsVINBox.Text = "";
            CarsModelBox.Text = "";
            CarsColorBox.Text = "";
            MobileStorageBox.Text = "";
            MobileColorBox.Text = "";

            LoadProduct();

            _ = _sync.PushAllDirtyAsync();        
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncProductsFromCloudAsync()
    {
        try
        {
            var cloudProducts = new CloudProductsInstallmentService(_supabaseService);
            var rows = await cloudProducts.GetProductsAsync();

            foreach (var row in rows)
            {
                var ownerLocalId = _owners.GetLocalIdByCloudId(row.OwnerId);

                if (ownerLocalId == 0)
                    continue;

                _products.UpsertFromCloud(
                    row.Id,
                    ownerLocalId,
                    row.ProductName,
                    row.ProductType,
                    row.ProductMainPrice,
                    row.CarPlateNumber,
                    row.CarVIN,
                    row.CarModel,
                    row.CarColor,
                    row.MobileStorage,
                    row.MobileColor
                );
            }

            LoadProduct();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment products cloud sync.");
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

        _ = _sync.PushAllDirtyAsync();
        _ = SyncProductsFromCloudAsync();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is ProductInstallment product)
            new ProductWindowViewInstallment(product.Id, _supabaseService).Show();
    }
}