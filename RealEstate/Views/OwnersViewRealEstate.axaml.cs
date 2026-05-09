using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Net.Http;
using System.Threading.Tasks;
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
        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadOwners();
        
        _ = _sync.PushAllDirtyAsync();
        _ = SyncOwnersFromCloudAsync();
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

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
        
        _ = _sync.PushAllDirtyAsync();
        _ = SyncOwnersFromCloudAsync();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is OwnerRealEstate owner)
            new OwnersWindowViewRealEstate(owner, _supabaseService).Show();
    }
}