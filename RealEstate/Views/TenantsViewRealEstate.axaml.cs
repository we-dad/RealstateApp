using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading;
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
        // Search box above the grid: shows the rows that contain every word typed.
        _gridSearch = new GridSearch<TenantRealEstate>(TenantsGrid, TenantsGridSearchBox);
        _supabaseService = supabaseService;

        _db.Initialize();
        _tenants = new TenantServiceRealEstate(_db);
        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadTenants,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadTenants();
        
        _ = SyncAsync();
    }

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadTenants() =>
        ScreenAutoRefresh.ReloadKeepingSelection<TenantRealEstate>(TenantsGrid, r => r.Id, LoadTenantsCore, () => _gridSearch?.AfterLoad());

    private void LoadTenantsCore()
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

    // push must finish before the pull, or the pull re-reads rows the
    // push has not written CloudIds for yet and duplicates them
    //
    // One sync at a time for this screen (static: also across an old and a new instance
    // of the screen). Two overlapping pulls could both insert the same new cloud row.
    // A request from the user (opening the screen, the refresh button) WAITS for its
    // turn, so it is never lost.
    private static readonly SemaphoreSlim _syncGate = new SemaphoreSlim(1, 1);

    private async Task SyncAsync()
    {
        await _syncGate.WaitAsync();

        try
        {
            await _sync.PushAllDirtyAsync();
            await SyncTenantsFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

    private async Task SyncTenantsFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

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

            SyncStatusService.ReportPull(true);
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping tenants cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    private ScreenAutoRefresh? _autoRefresh;
    private GridSearch<TenantRealEstate>? _gridSearch;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadTenants();
        
        _ = SyncAsync();
    }

    private void TenantsGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (TenantsGrid.SelectedItem is not TenantRealEstate tenant)
            return;

        var window = new TenantsWindowViewRealEstate(tenant, _supabaseService);
        window.Show();
    }
}