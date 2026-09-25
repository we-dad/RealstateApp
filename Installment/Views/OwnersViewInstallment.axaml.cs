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
    private readonly Action? _onOpenContracts;

    // onOpenContracts: switches MainWindowInstallment to its Contracts tab (the
    // per-row "العقود" shortcut button). Optional because this view has no
    // access to the parent shell otherwise - null just hides/no-ops the button's
    // effect instead of crashing if this view is ever hosted without a shell.
    public OwnersViewInstallment(SupabaseService supabaseService, Action? onOpenContracts = null)
    {
        InitializeComponent();
        _onOpenContracts = onOpenContracts;
        // Search box above the grid: shows the rows that contain every word typed.
        _gridSearch = new GridSearch<OwnerInstallment>(OwnersGrid, OwnersGridSearchBox);
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

            OwnersCountText.Text = $"{data.Count} ملاك";
        }, () => _gridSearch?.AfterLoad());
    
    // Unlike the rest of the app (inline add-form on the list screen), Owners now
    // adds through its own window, per the developer's explicit request to match
    // the reference design's separate owners.html/owner-new.html pages. The new
    // window calls OwnerInstallmentService.Add() itself, which raises
    // DataChangeNotifier - _autoRefresh already listens for that and reloads the
    // grid, so no explicit LoadOwners()/PushAllDirtyAsync() call is needed here.
    private void AddOwner_Click(object? sender, RoutedEventArgs e)
    {
        new OwnersAddWindowViewInstallment(_supabaseService).Show();
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
    private GridSearch<OwnerInstallment>? _gridSearch;

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();

        _ = SyncAsync();
    }

    private void OwnersGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (OwnersGrid.SelectedItem is OwnerInstallment owner)
            OpenOwnerWindow(owner);
    }

    // Per-row "تعديل" button - same edit window double-clicking the row already
    // opens, just also reachable without a double-click now (matches the mockup).
    private void EditOwner_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: OwnerInstallment owner })
            OpenOwnerWindow(owner);
    }

    private void OpenOwnerWindow(OwnerInstallment owner) =>
        new OwnersWindowViewInstallment(owner, _supabaseService).Show();

    // Per-row "العقود" shortcut - just switches to the Contracts tab. Contracts
    // has no per-owner filter yet, so this does not actually filter to this
    // owner's contracts, only saves a click to get there.
    private void OpenContracts_Click(object? sender, RoutedEventArgs e)
    {
        _onOpenContracts?.Invoke();
    }
}
