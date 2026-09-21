using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class OwnersViewInstallment : UserControl
{
    private readonly DbServiceInstallment _db = new DbServiceInstallment();
    private readonly OwnerInstallmentService _owners;
    private readonly SupabaseService _supabaseService;
    private readonly InstallmentSyncService _sync;
    
    public OwnersViewInstallment(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();
        _owners = new OwnerInstallmentService(_db);
        
        LoadOwners();

        // Reload the grid (only) when data changes: an add, an edit or a delete, also
        // from the details window, so there is no need to press "تحديث".
        // Every 2 minutes (while the app is active) push then pull, so other users'
        // changes appear by themselves.
        _autoRefresh = new ScreenAutoRefresh(
            this,
            LoadOwners,
            periodicSync: PeriodicSyncAsync,
            interval: TimeSpan.FromMinutes(2));

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = SyncAsync();
        
        OwnersGrid.DoubleTapped += OwnersGrid_DoubleTapped;
    }

    // Every reload (open, add, pull, timer) keeps the selected row selected, chosen at the
    // moment the grid is replaced, so a row picked while a sync runs is not undone.
    private void LoadOwners() =>
        ScreenAutoRefresh.ReloadKeepingSelection<OwnerInstallment>(OwnersGrid, r => r.Id, () =>
        {
            var data = _owners.GetAll();

            OwnersGrid.ItemsSource = null;
            OwnersGrid.ItemsSource = data;
        });
    
    private void Add_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var name = NameBox.Text?.Trim() ?? "";
            var phone = PhoneBox.Text?.Trim() ?? "";
            var identityNumber = IdentityNumberBox.Text?.Trim() ?? "";
            var address = AddressBox.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(name))
                return;

            _owners.Add(name, identityNumber, phone, address);

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

    // The timer is not needed while a sync is already running: skip it.
    private Task PeriodicSyncAsync() =>
        _syncGate.CurrentCount == 0 ? Task.CompletedTask : SyncAsync();

    private async Task SyncOwnersFromCloudAsync()
    {
        if (!AppSession.CanReadOnline)
            return;

        try
        {
            var cloud = new CloudOwnersInstallmentService(_supabaseService);
            var rows = await cloud.GetOwnersAsync();

            foreach (var row in rows)
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
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment owners cloud sync.");
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

    private void OwnersGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (OwnersGrid.SelectedItem is OwnerInstallment owner)
            new OwnersWindowViewInstallment(owner, _supabaseService).Show();
    }
}