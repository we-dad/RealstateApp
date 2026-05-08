using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class TenantsViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly TenantServiceRealEstate _tenants;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public TenantsViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _tenants = new TenantServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadTenants();
        
        _ = _sync.PushAllDirtyAsync();
        _ = SyncTenantsFromCloudAsync();
    }

    private void LoadTenants()
    {
        var data = _tenants.GetAll();

        Console.WriteLine($"Tenants count: {data.Count}");

        TenantsGrid.ItemsSource = null;
        TenantsGrid.ItemsSource = data;
    }
    
    private async void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _tenants.Add(name, identityNumber, phone, address);

            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";

            LoadTenants();

            _ = _sync.PushAllDirtyAsync();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncTenantsFromCloudAsync()
    {
        try
        {
            var cloudTenants = new CloudTenantsRealEstateService(_supabaseService);
            var cloudRows = await cloudTenants.GetTenantsAsync();

            foreach (var row in cloudRows)
            {
                _tenants.UpsertFromCloud(
                    row.Id,
                    row.Name,
                    row.IdentityNumber,
                    row.Phone,
                    row.Address
                );
            }

            LoadTenants();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping tenants cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadTenants();
        
        _ = _sync.PushAllDirtyAsync();
        _ = SyncTenantsFromCloudAsync();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TenantRealEstate tenant)
            new TenantsWindowViewRealEstate(tenant, _supabaseService).Show();
    }
}