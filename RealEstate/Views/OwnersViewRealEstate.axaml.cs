using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Input;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _owners;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public OwnersViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();

        _supabaseService = supabaseService;

        _db.Initialize();
        _owners = new OwnerServiceRealEstate(_db);
        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadOwners,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadOwners();
        
        _ = SyncAsync();
    }

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at
    // the moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadOwners() =>
        ScreenAutoRefresh.ReloadKeepingSelection<OwnerRealEstate>(OwnersGrid, r => r.Id, LoadOwnersCore);

    private void LoadOwnersCore()
    {
        var data = _owners.GetAll();
        
        OwnersGrid.ItemsSource = null;
        OwnersGrid.ItemsSource = data;
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

            // Save local only. Add() marks IsDirty = 1, SyncAction = "insert"
            _owners.Add(
                name,
                identityNumber,
                phone,
                address
            );
            
            NameBox.Text = "";
            IdentityNumberBox.Text = "";
            PhoneBox.Text = "";
            AddressBox.Text = "";

            LoadOwners();

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
            await SyncOwnersFromCloudAsync();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    // Every 2 minutes (while the app is active); skipped while a sync is already running.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

    private async Task SyncOwnersFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloudOwners = new CloudOwnersRealEstateService(_supabaseService);
            var cloudRows = await cloudOwners.GetOwnersAsync();

            foreach (var row in cloudRows)
            {
                _owners.UpsertFromCloud(
                    row.Id,
                    row.Name,
                    row.IdentityNumber,
                    row.Phone,
                    row.Address
                );
            }

            LoadOwners();

            SyncStatusService.ReportPull(true);
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Offline: skipping owners cloud sync.");
            SyncStatusService.ReportPull(false, offline: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            SyncStatusService.ReportPull(false);
        }
    }

    private ScreenAutoRefresh? _autoRefresh;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
        
        _ = SyncAsync();
    }

    private void OwnersGrid_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (OwnersGrid.SelectedItem is not OwnerRealEstate owner)
            return;

        var window = new OwnersWindowViewRealEstate(owner, _supabaseService);

        window.Show();
    }
}