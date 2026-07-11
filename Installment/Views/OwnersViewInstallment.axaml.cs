using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
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

        _sync = new InstallmentSyncService(_db, _supabaseService);
        _ = _sync.PushAllDirtyAsync();
        _ = SyncOwnersFromCloudAsync();
        
        OwnersGrid.DoubleTapped += OwnersGrid_DoubleTapped;
    }

    private void LoadOwners()
    {
        var data = _owners.GetAll();

        OwnersGrid.ItemsSource = null;
        OwnersGrid.ItemsSource = data;
    }
    
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
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping installment owners cloud sync.");
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

    private void OwnersGrid_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (OwnersGrid.SelectedItem is OwnerInstallment owner)
            new OwnersWindowViewInstallment(owner, _supabaseService).Show();
    }
}