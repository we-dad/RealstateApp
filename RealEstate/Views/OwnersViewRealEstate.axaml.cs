using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Net.Http;
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
        _autoRefresh = new ScreenAutoRefresh(this, () =>
            ScreenAutoRefresh.ReloadKeepingSelection<OwnerRealEstate>(OwnersGrid, r => r.Id, LoadOwners));

        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadOwners();
        
        _ = SyncAsync();
    }

    private void LoadOwners()
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
    private async Task SyncAsync()
    {
        await _sync.PushAllDirtyAsync();
        await SyncOwnersFromCloudAsync();
    }

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
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Offline: skipping owners cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
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