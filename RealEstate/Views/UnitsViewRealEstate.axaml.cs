using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstateInstallmentsManager.Models;
using RealEstateInstallmentsManager.Models.Cloud;
using RealEstateInstallmentsManager.Services;

namespace RealEstateInstallmentsManager.Views;

public partial class UnitsViewRealEstate : UserControl
{
    private readonly DbServiceRealEstate _db = new DbServiceRealEstate();
    private readonly OwnerServiceRealEstate _owners;
    private readonly UnitServiceRealEstate _units;
    private readonly SupabaseService _supabaseService;
    private readonly RealEstateSyncService _sync;

    public UnitsViewRealEstate(SupabaseService supabaseService)
    {
        InitializeComponent();
        _supabaseService = supabaseService;

        _db.Initialize();

        _owners = new OwnerServiceRealEstate(_db);
        _units = new UnitServiceRealEstate(_db);
        _sync = new RealEstateSyncService(_db, _supabaseService);

        LoadOwners();
        LoadUnitTypes();
        LoadUnits();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncUnitsFromCloudAsync();
    }

    private void LoadOwners()
    {
        var owners = _owners.GetAll();

        OwnerBox.ItemsSource = owners;

        if (owners.Count > 0)
            OwnerBox.SelectedIndex = 0;
    }

    private void LoadUnitTypes()
    {
        UnitTypeBox.ItemsSource = new List<string>
        {
            "سكني",
            "تجاري"
        };

        UnitTypeBox.SelectedIndex = 0;
    }

    private void LoadUnits()
    {
        try
        {
            var data = _units.GetAll();
            UnitsGrid.ItemsSource = null;
            UnitsGrid.ItemsSource = data;
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
            if (OwnerBox.SelectedItem is not OwnerRealEstate owner)
                return;

            var city = CityBox.Text?.Trim() ?? "";
            var unitName = UnitNameBox.Text?.Trim() ?? "";
            var district = DistrictBox.Text?.Trim() ?? "";
            var unitType = UnitTypeBox.SelectedItem as string ?? "سكني";

            var unitsCountText = UnitsCountBox.Text?.Trim() ?? "1";

            if (!int.TryParse(unitsCountText, out var unitsCount) || unitsCount < 1)
                unitsCount = 1;

            for (int i = 1; i <= unitsCount; i++)
            {
                var unitNum = i;
                var finalUnitName = unitName + "-" + i;

                _units.Add(
                    owner.Id,
                    finalUnitName,
                    city,
                    district,
                    unitType,
                    unitsCount,
                    unitNum
                );
            }

            UnitNameBox.Text = "";
            CityBox.Text = "";
            DistrictBox.Text = "";
            UnitTypeBox.SelectedIndex = 0;
            UnitsCountBox.Text = "1";

            LoadUnits();

            _ = _sync.PushAllDirtyAsync();
            
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SyncUnitsFromCloudAsync()
    {
        try
        {
            var cloudUnits = new CloudUnitsRealEstateService(_supabaseService);
            var cloudRows = await cloudUnits.GetUnitsAsync();

            foreach (var row in cloudRows)
            {
                var ownerLocalId = _owners.GetLocalIdByCloudId(row.OwnerId);

                if (ownerLocalId == 0)
                    continue;

                _units.UpsertFromCloud(
                    row.Id,
                    ownerLocalId,
                    row.UnitName,
                    row.City,
                    row.District,
                    row.UnitType,
                    row.UnitState,
                    row.UnitsCount,
                    row.UnitNum
                );
            }

            LoadUnits();
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Console.WriteLine("Offline: skipping units cloud sync.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private void Refresh_Click(object? sender, RoutedEventArgs e)
    {
        LoadOwners();
        LoadUnits();

        _ = _sync.PushAllDirtyAsync();
        _ = SyncUnitsFromCloudAsync();
    }

    private void OpenInfoWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is UnitRealEstate unit)
            new UnitsWindowViewRealEstate(unit.Id, _supabaseService).Show();
    }
}